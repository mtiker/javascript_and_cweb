using App.BLL.Contracts.Finance;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class InvoiceService(AppDbContext dbContext, ITenantAccessService tenantAccessService) : IInvoiceService
{
    public async Task<IReadOnlyCollection<InvoiceSummaryResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var query = dbContext.Invoices
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        var invoices = await query
            .OrderByDescending(entity => entity.DueDateUtc)
            .ToListAsync(cancellationToken);

        return invoices.Select(ToSummaryResult).ToList();
    }

    public async Task<InvoiceDetailResult> GetAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var invoice = await LoadInvoiceAsync(invoiceId, asNoTracking: true, cancellationToken);
        if (invoice == null)
        {
            throw new NotFoundException("Invoice was not found.");
        }

        return ToDetailResult(invoice);
    }

    public async Task<InvoiceDetailResult> CreateAsync(Guid userId, CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        await ValidateInvoiceHeaderAsync(
            command.PatientId,
            command.CostEstimateId,
            command.InvoiceNumber,
            null,
            cancellationToken);

        var invoice = new Invoice
        {
            PatientId = command.PatientId,
            CostEstimateId = command.CostEstimateId,
            InvoiceNumber = command.InvoiceNumber.Trim(),
            DueDateUtc = command.DueDateUtc,
            Status = InvoiceStatus.Draft
        };

        foreach (var line in await BuildInvoiceLinesAsync(command.PatientId, command.Lines, cancellationToken))
        {
            invoice.Lines.Add(line);
        }

        FinanceMath.ApplyInvoiceState(invoice);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadInvoiceAsync(invoice.Id, asNoTracking: true, cancellationToken);
        return ToDetailResult(saved!);
    }

    public async Task<InvoiceDetailResult> GenerateFromProceduresAsync(
        Guid userId,
        GenerateInvoiceFromProceduresCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        await ValidateInvoiceHeaderAsync(
            command.PatientId,
            command.CostEstimateId,
            command.InvoiceNumber,
            null,
            cancellationToken);

        if (command.TreatmentIds.Count == 0)
        {
            throw new ValidationAppException("Select at least one performed procedure.");
        }

        var treatmentIds = command.TreatmentIds.Distinct().ToArray();
        var treatments = await dbContext.Treatments
            .AsNoTracking()
            .Include(entity => entity.TreatmentType)
            .Where(entity => treatmentIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        if (treatments.Count != treatmentIds.Length)
        {
            throw new ValidationAppException("One or more performed procedures do not exist in current company.");
        }

        if (treatments.Any(entity => entity.PatientId != command.PatientId))
        {
            throw new ValidationAppException("All performed procedures must belong to the selected patient.");
        }

        var invoice = new Invoice
        {
            PatientId = command.PatientId,
            CostEstimateId = command.CostEstimateId,
            InvoiceNumber = command.InvoiceNumber.Trim(),
            DueDateUtc = command.DueDateUtc,
            Status = InvoiceStatus.Draft
        };

        foreach (var treatment in treatments.OrderBy(entity => entity.PerformedAtUtc))
        {
            var line = new InvoiceLine
            {
                TreatmentId = treatment.Id,
                PlanItemId = treatment.PlanItemId,
                Description = BuildTreatmentDescription(treatment),
                Quantity = 1m,
                UnitPrice = treatment.Price,
                CoverageAmount = 0m
            };

            FinanceMath.NormalizeInvoiceLine(line);
            invoice.Lines.Add(line);
        }

        FinanceMath.ApplyInvoiceState(invoice);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadInvoiceAsync(invoice.Id, asNoTracking: true, cancellationToken);
        return ToDetailResult(saved!);
    }

    public async Task<PaymentResult> AddPaymentAsync(
        Guid userId,
        Guid invoiceId,
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var invoice = await LoadInvoiceAsync(invoiceId, asNoTracking: false, cancellationToken);
        if (invoice == null)
        {
            throw new NotFoundException("Invoice was not found.");
        }

        FinanceMath.ApplyInvoiceState(invoice);
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ValidationAppException("Payments cannot be posted to a cancelled invoice.");
        }

        if (command.Amount > invoice.BalanceAmount && !FinanceMath.AmountsMatch(command.Amount, invoice.BalanceAmount))
        {
            throw new ValidationAppException("Payment amount cannot exceed the current invoice balance.");
        }

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = FinanceMath.RoundAmount(command.Amount),
            PaidAtUtc = command.PaidAtUtc,
            Method = command.Method.Trim(),
            Reference = NormalizeOptional(command.Reference),
            Notes = NormalizeOptional(command.Notes)
        };

        invoice.Payments.Add(payment);
        FinanceMath.ApplyInvoiceState(invoice);
        if (invoice.PaymentPlan != null)
        {
            FinanceMath.ApplyPaymentPlanState(invoice.PaymentPlan, invoice.Payments);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToPaymentResult(payment);
    }

    public async Task<InvoiceDetailResult> UpdateAsync(Guid userId, UpdateInvoiceCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var invoice = await LoadInvoiceAsync(command.InvoiceId, asNoTracking: false, cancellationToken);
        if (invoice == null)
        {
            throw new NotFoundException("Invoice was not found.");
        }

        await ValidateInvoiceHeaderAsync(
            command.PatientId,
            command.CostEstimateId,
            command.InvoiceNumber,
            command.InvoiceId,
            cancellationToken);

        if (invoice.Payments.Count > 0 || invoice.PaymentPlan != null)
        {
            throw new ValidationAppException("Invoice lines cannot be replaced after payments or payment plans exist.");
        }

        var lines = await BuildInvoiceLinesAsync(command.PatientId, command.Lines, cancellationToken);

        dbContext.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines.Clear();

        foreach (var line in lines)
        {
            invoice.Lines.Add(line);
        }

        invoice.PatientId = command.PatientId;
        invoice.CostEstimateId = command.CostEstimateId;
        invoice.InvoiceNumber = command.InvoiceNumber.Trim();
        invoice.DueDateUtc = command.DueDateUtc;
        FinanceMath.ApplyInvoiceState(invoice);

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadInvoiceAsync(invoice.Id, asNoTracking: true, cancellationToken);
        return ToDetailResult(saved!);
    }

    public async Task DeleteAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var invoice = await LoadInvoiceAsync(invoiceId, asNoTracking: false, cancellationToken);
        if (invoice == null)
        {
            throw new NotFoundException("Invoice was not found.");
        }

        if (invoice.PaymentPlan != null)
        {
            dbContext.PaymentPlanInstallments.RemoveRange(invoice.PaymentPlan.Installments);
            dbContext.PaymentPlans.Remove(invoice.PaymentPlan);
        }

        dbContext.Payments.RemoveRange(invoice.Payments);
        dbContext.InvoiceLines.RemoveRange(invoice.Lines);
        dbContext.Invoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static InvoiceSummaryResult ToSummaryResult(Invoice entity)
    {
        return new InvoiceSummaryResult(
            entity.Id,
            entity.PatientId,
            entity.CostEstimateId,
            entity.InvoiceNumber,
            entity.TotalAmount,
            entity.Lines.Sum(line => line.CoverageAmount),
            entity.Lines.Sum(line => line.PatientAmount),
            entity.Payments.Sum(payment => payment.Amount),
            entity.BalanceAmount,
            entity.DueDateUtc,
            entity.Status.ToString());
    }

    internal static InvoiceDetailResult ToDetailResult(Invoice entity)
    {
        return new InvoiceDetailResult(
            entity.Id,
            entity.PatientId,
            entity.CostEstimateId,
            entity.InvoiceNumber,
            entity.TotalAmount,
            entity.Lines.Sum(line => line.CoverageAmount),
            entity.Lines.Sum(line => line.PatientAmount),
            entity.Payments.Sum(payment => payment.Amount),
            entity.BalanceAmount,
            entity.DueDateUtc,
            entity.Status.ToString(),
            entity.Lines
                .OrderBy(line => line.CreatedAtUtc)
                .Select(ToInvoiceLineResult)
                .ToArray(),
            entity.Payments
                .OrderByDescending(payment => payment.PaidAtUtc)
                .Select(ToPaymentResult)
                .ToArray(),
            entity.PaymentPlan == null
                ? null
                : ToPaymentPlanResult(entity.PaymentPlan, entity.BalanceAmount));
    }

    internal static PaymentPlanResult ToPaymentPlanResult(PaymentPlan entity, decimal remainingAmount)
    {
        return new PaymentPlanResult(
            entity.Id,
            entity.InvoiceId,
            entity.StartsAtUtc,
            entity.Status.ToString(),
            entity.Terms,
            entity.Installments.Sum(installment => installment.Amount),
            remainingAmount,
            entity.Installments
                .OrderBy(installment => installment.DueDateUtc)
                .Select(installment => new PaymentPlanInstallmentResult(
                    installment.Id,
                    installment.DueDateUtc,
                    installment.Amount,
                    installment.Status.ToString(),
                    installment.PaidAtUtc))
                .ToArray());
    }

    private Task EnsureAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager);
    }

    private async Task ValidateInvoiceHeaderAsync(
        Guid patientId,
        Guid? costEstimateId,
        string invoiceNumber,
        Guid? currentInvoiceId,
        CancellationToken cancellationToken)
    {
        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == patientId, cancellationToken);
        if (!patientExists)
        {
            throw new ValidationAppException("Patient does not exist in current company.");
        }

        if (costEstimateId.HasValue)
        {
            var estimate = await dbContext.CostEstimates
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == costEstimateId.Value, cancellationToken);
            if (estimate == null)
            {
                throw new ValidationAppException("Cost estimate does not exist in current company.");
            }

            if (estimate.PatientId != patientId)
            {
                throw new ValidationAppException("Cost estimate must belong to the same patient as the invoice.");
            }
        }

        var normalizedInvoiceNumber = invoiceNumber.Trim();
        var duplicateNumber = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(
                entity => entity.InvoiceNumber == normalizedInvoiceNumber &&
                          (!currentInvoiceId.HasValue || entity.Id != currentInvoiceId.Value),
                cancellationToken);
        if (duplicateNumber)
        {
            throw new ValidationAppException("Invoice number already exists.");
        }
    }

    private async Task<List<InvoiceLine>> BuildInvoiceLinesAsync(
        Guid patientId,
        IReadOnlyCollection<InvoiceLineCommand> requestLines,
        CancellationToken cancellationToken)
    {
        if (requestLines.Count == 0)
        {
            throw new ValidationAppException("At least one invoice line is required.");
        }

        var treatmentIds = requestLines
            .Where(entity => entity.TreatmentId.HasValue)
            .Select(entity => entity.TreatmentId!.Value)
            .Distinct()
            .ToArray();

        var treatmentLookup = treatmentIds.Length == 0
            ? new Dictionary<Guid, Treatment>()
            : await dbContext.Treatments
                .AsNoTracking()
                .Include(entity => entity.TreatmentType)
                .Where(entity => treatmentIds.Contains(entity.Id))
                .ToDictionaryAsync(entity => entity.Id, cancellationToken);

        if (treatmentLookup.Count != treatmentIds.Length)
        {
            throw new ValidationAppException("One or more performed procedures are invalid.");
        }

        var planItemIds = requestLines
            .Where(entity => entity.PlanItemId.HasValue)
            .Select(entity => entity.PlanItemId!.Value)
            .Distinct()
            .ToArray();

        var planItemLookup = planItemIds.Length == 0
            ? new Dictionary<Guid, PlanItemInvoiceLookup>()
            : await dbContext.PlanItems
                .AsNoTracking()
                .Where(entity => planItemIds.Contains(entity.Id))
                .Join(
                    dbContext.TreatmentPlans.AsNoTracking(),
                    item => item.TreatmentPlanId,
                    plan => plan.Id,
                    (item, plan) => new { item, plan })
                .Join(
                    dbContext.TreatmentTypes.AsNoTracking(),
                    pair => pair.item.TreatmentTypeId,
                    type => type.Id,
                    (pair, type) => new PlanItemInvoiceLookup(
                        pair.item.Id,
                        pair.plan.PatientId,
                        pair.item.EstimatedPrice,
                        type.Name))
                .ToDictionaryAsync(entity => entity.Id, cancellationToken);

        if (planItemLookup.Count != planItemIds.Length)
        {
            throw new ValidationAppException("One or more plan items are invalid.");
        }

        var lines = new List<InvoiceLine>(requestLines.Count);

        foreach (var requestLine in requestLines)
        {
            Treatment? treatment = null;
            if (requestLine.TreatmentId.HasValue)
            {
                treatment = treatmentLookup[requestLine.TreatmentId.Value];
                if (treatment.PatientId != patientId)
                {
                    throw new ValidationAppException("Performed procedure does not belong to the selected patient.");
                }
            }

            PlanItemInvoiceLookup? planItem = null;
            if (requestLine.PlanItemId.HasValue)
            {
                planItem = planItemLookup[requestLine.PlanItemId.Value];
                if (planItem.PatientId != patientId)
                {
                    throw new ValidationAppException("Plan item does not belong to the selected patient.");
                }
            }

            if (treatment?.PlanItemId.HasValue == true &&
                requestLine.PlanItemId.HasValue &&
                treatment.PlanItemId.Value != requestLine.PlanItemId.Value)
            {
                throw new ValidationAppException("Performed procedure plan item does not match the selected plan item.");
            }

            var quantity = requestLine.Quantity <= 0m ? 1m : requestLine.Quantity;
            var unitPrice = requestLine.UnitPrice > 0m
                ? requestLine.UnitPrice
                : treatment?.Price ?? planItem?.EstimatedPrice ?? 0m;

            var description = string.IsNullOrWhiteSpace(requestLine.Description)
                ? treatment != null
                    ? BuildTreatmentDescription(treatment)
                    : planItem != null
                        ? $"Planned {planItem.TreatmentTypeName}"
                        : null
                : requestLine.Description.Trim();

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ValidationAppException("Invoice line description is required when no treatment or plan item is linked.");
            }

            var lineTotal = quantity * unitPrice;
            if (requestLine.CoverageAmount > lineTotal)
            {
                throw new ValidationAppException("Invoice line coverage cannot exceed the line total.");
            }

            var line = new InvoiceLine
            {
                TreatmentId = treatment?.Id,
                PlanItemId = requestLine.PlanItemId ?? treatment?.PlanItemId,
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice,
                CoverageAmount = requestLine.CoverageAmount
            };

            FinanceMath.NormalizeInvoiceLine(line);
            lines.Add(line);
        }

        return lines;
    }

    private async Task<Invoice?> LoadInvoiceAsync(Guid invoiceId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Invoices
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .Include(entity => entity.PaymentPlan)
            .ThenInclude(entity => entity!.Installments)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);
    }

    private static InvoiceLineResult ToInvoiceLineResult(InvoiceLine entity)
    {
        return new InvoiceLineResult(
            entity.Id,
            entity.TreatmentId,
            entity.PlanItemId,
            entity.Description,
            entity.Quantity,
            entity.UnitPrice,
            entity.LineTotal,
            entity.CoverageAmount,
            entity.PatientAmount);
    }

    private static PaymentResult ToPaymentResult(Payment entity)
    {
        return new PaymentResult(
            entity.Id,
            entity.InvoiceId,
            entity.Amount,
            entity.PaidAtUtc,
            entity.Method,
            entity.Reference,
            entity.Notes);
    }

    private static string BuildTreatmentDescription(Treatment entity)
    {
        var baseName = entity.TreatmentType?.Name ?? "Procedure";
        return entity.ToothNumber.HasValue
            ? $"{baseName} - tooth {entity.ToothNumber.Value}"
            : baseName;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record PlanItemInvoiceLookup(Guid Id, Guid PatientId, decimal EstimatedPrice, string TreatmentTypeName);
}
