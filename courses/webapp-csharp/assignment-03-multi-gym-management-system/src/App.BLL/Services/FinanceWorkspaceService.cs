using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Finance;

namespace App.BLL.Services;

public class FinanceWorkspaceService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IUserContextService userContextService,
    IMembershipFinanceMapper mapper) : IFinanceWorkspaceService
{
    public async Task<FinanceWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                            ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");

        return await BuildWorkspaceAsync(gymId, currentMember.Id, cancellationToken);
    }

    public async Task<FinanceWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member,
            RoleNames.SystemBilling);

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId, cancellationToken);

        return await BuildWorkspaceAsync(gymId, memberId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvoiceResponse>> GetInvoicesAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        if (memberId.HasValue)
        {
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId.Value, cancellationToken);
        }

        var effectiveMemberId = memberId;
        if (userContextService.GetCurrent().HasRole(RoleNames.Member))
        {
            var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                                ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");
            effectiveMemberId = currentMember.Id;
        }

        var invoices = await unitOfWork.Finance.ListInvoicesAsync(gymId, effectiveMemberId, cancellationToken);
        return mapper.ToInvoiceResponses(invoices);
    }

    public async Task<InvoiceResponse> GetInvoiceAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var invoice = await unitOfWork.Finance.FindInvoiceAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Invoice was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, invoice.MemberId, cancellationToken);

        return mapper.ToInvoiceResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(string gymCode, InvoiceCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (await unitOfWork.Members.FindAsync(gymId, request.MemberId, cancellationToken) is null)
        {
            throw new ValidationAppException("Member was not found in the active gym.");
        }

        var lines = ParseInvoiceLines(gymId, request.Lines);
        var subtotal = lines.Where(entity => !entity.IsCredit).Sum(entity => entity.LineTotal);
        var credits = lines.Where(entity => entity.IsCredit).Sum(entity => entity.LineTotal);
        var total = Math.Max(0m, subtotal - credits);

        var invoice = new Invoice
        {
            GymId = gymId,
            MemberId = request.MemberId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(gymId, cancellationToken),
            IssuedAtUtc = DateTime.UtcNow,
            DueAtUtc = request.DueAtUtc,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "EUR" : request.CurrencyCode.Trim().ToUpperInvariant(),
            SubtotalAmount = subtotal,
            CreditAmount = credits,
            TotalAmount = total,
            PaidAmount = 0m,
            OutstandingAmount = total,
            Status = total == 0m ? InvoiceStatus.Paid : InvoiceStatus.Issued,
            Notes = request.Notes?.Trim(),
            Lines = lines
        };

        await unitOfWork.Finance.AddInvoiceAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.Finance.FindInvoiceAsync(gymId, invoice.Id, cancellationToken)
                    ?? throw new NotFoundException("Invoice was not found after creation.");

        return mapper.ToInvoiceResponse(saved);
    }

    public Task<InvoiceResponse> AddInvoicePaymentAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoiceAdjustmentAsync(gymCode, id, request, false, cancellationToken);

    public Task<InvoiceResponse> AddInvoiceRefundAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoiceAdjustmentAsync(gymCode, id, request, true, cancellationToken);

    private async Task<InvoiceResponse> AddInvoiceAdjustmentAsync(
        string gymCode,
        Guid id,
        InvoicePaymentRequest request,
        bool isRefund,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var invoice = await unitOfWork.Finance.FindInvoiceAsync(gymId, id, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, invoice.MemberId, cancellationToken);

        if (request.Amount <= 0)
        {
            throw new ValidationAppException("Amount must be greater than zero.");
        }

        if (isRefund && request.Amount > invoice.PaidAmount)
        {
            throw new ValidationAppException("Refund amount cannot exceed paid amount.");
        }

        if (!isRefund && request.Amount > invoice.OutstandingAmount)
        {
            throw new ValidationAppException("Payment amount cannot exceed outstanding amount.");
        }

        var paymentRecord = new Payment
        {
            GymId = gymId,
            MembershipId = null,
            BookingId = null,
            Amount = request.Amount,
            CurrencyCode = invoice.CurrencyCode,
            Reference = request.Reference?.Trim(),
            Status = isRefund ? PaymentStatus.Refunded : PaymentStatus.Completed
        };

        await unitOfWork.Finance.AddPaymentAsync(paymentRecord, cancellationToken);

        var invoicePayment = new InvoicePayment
        {
            GymId = gymId,
            InvoiceId = invoice.Id,
            Payment = paymentRecord,
            Amount = request.Amount,
            IsRefund = isRefund,
            AppliedAtUtc = DateTime.UtcNow,
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim()
        };

        await unitOfWork.Finance.AddInvoicePaymentAsync(invoicePayment, cancellationToken);

        if (isRefund)
        {
            invoice.PaidAmount = Math.Max(0m, invoice.PaidAmount - request.Amount);
            invoice.OutstandingAmount = Math.Min(invoice.TotalAmount, invoice.OutstandingAmount + request.Amount);
        }
        else
        {
            invoice.PaidAmount = Math.Min(invoice.TotalAmount, invoice.PaidAmount + request.Amount);
            invoice.OutstandingAmount = Math.Max(0m, invoice.OutstandingAmount - request.Amount);
        }

        invoice.Status = ResolveInvoiceStatus(invoice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.Finance.FindInvoiceAsync(gymId, invoice.Id, cancellationToken)
                    ?? throw new NotFoundException("Invoice was not found after payment update.");

        return mapper.ToInvoiceResponse(saved);
    }

    private async Task<FinanceWorkspaceResponse> BuildWorkspaceAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken)
    {
        var member = await unitOfWork.Members.FindWithPersonAsync(gymId, memberId, cancellationToken)
            ?? throw new NotFoundException("Member was not found.");

        var invoices = await unitOfWork.Finance.ListInvoicesAsync(gymId, memberId, cancellationToken);
        return mapper.ToFinanceWorkspace(member, mapper.ToInvoiceResponses(invoices));
    }

    private static List<InvoiceLine> ParseInvoiceLines(Guid gymId, IReadOnlyCollection<InvoiceLineRequest> lines)
    {
        if (lines.Count == 0)
        {
            throw new ValidationAppException("At least one invoice line is required.");
        }

        return lines.Select(line =>
        {
            if (string.IsNullOrWhiteSpace(line.Description))
            {
                throw new ValidationAppException("Invoice line description is required.");
            }

            if (line.Quantity <= 0)
            {
                throw new ValidationAppException("Invoice line quantity must be greater than zero.");
            }

            if (line.UnitPrice < 0)
            {
                throw new ValidationAppException("Invoice line unit price cannot be negative.");
            }

            return new InvoiceLine
            {
                GymId = gymId,
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero),
                IsCredit = line.IsCredit,
                Notes = line.Notes?.Trim()
            };
        }).ToList();
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var dayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var existingTodayCount = await unitOfWork.Finance.CountInvoicesIssuedBetweenAsync(gymId, dayStart, dayEnd, cancellationToken);

        return $"INV-{today:yyyyMMdd}-{existingTodayCount + 1:D4}";
    }

    private static InvoiceStatus ResolveInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return InvoiceStatus.Cancelled;
        }

        if (invoice.PaidAmount <= 0 && invoice.OutstandingAmount > 0 && invoice.DueAtUtc < DateTime.UtcNow)
        {
            return InvoiceStatus.Overdue;
        }

        if (invoice.OutstandingAmount <= 0)
        {
            return InvoiceStatus.Paid;
        }

        if (invoice.PaidAmount > 0)
        {
            return InvoiceStatus.PartiallyPaid;
        }

        return invoice.DueAtUtc < DateTime.UtcNow
            ? InvoiceStatus.Overdue
            : InvoiceStatus.Issued;
    }
}
