using App.BLL.Contracts.Finance;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class PaymentPlanService(
    AppDbContext dbContext,
    ITenantAccessService tenantAccessService,
    ISubscriptionPolicyService subscriptionPolicyService) : IPaymentPlanService
{
    public async Task<IReadOnlyCollection<PaymentPlanResult>> ListAsync(Guid userId, Guid? invoiceId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var query = dbContext.PaymentPlans
            .AsNoTracking()
            .Include(entity => entity.Installments)
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity!.Payments)
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity!.Lines)
            .AsQueryable();

        if (invoiceId.HasValue)
        {
            query = query.Where(entity => entity.InvoiceId == invoiceId.Value);
        }

        var plans = await query
            .OrderByDescending(entity => entity.StartsAtUtc)
            .ToListAsync(cancellationToken);

        return plans.Select(ToResult).ToList();
    }

    public async Task<PaymentPlanResult> GetAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var plan = await LoadPaymentPlanAsync(paymentPlanId, asNoTracking: true, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException("Payment plan was not found.");
        }

        return ToResult(plan);
    }

    public async Task<PaymentPlanResult> CreateAsync(Guid userId, CreatePaymentPlanCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var invoice = await dbContext.Invoices
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .Include(entity => entity.PaymentPlan)
            .SingleOrDefaultAsync(entity => entity.Id == command.InvoiceId, cancellationToken);
        if (invoice == null)
        {
            throw new ValidationAppException("Invoice does not exist in current company.");
        }

        if (invoice.PaymentPlan != null)
        {
            throw new ValidationAppException("Payment plan already exists for this invoice.");
        }

        FinanceMath.ApplyInvoiceState(invoice);
        if (invoice.BalanceAmount <= 0m)
        {
            throw new ValidationAppException("Payment plan requires an invoice with an outstanding balance.");
        }

        var installments = BuildInstallments(command.Installments);
        var scheduledAmount = installments.Sum(entity => entity.Amount);
        if (!FinanceMath.AmountsMatch(scheduledAmount, invoice.BalanceAmount))
        {
            throw new ValidationAppException("Payment plan installments must cover the invoice balance exactly.");
        }

        var plan = new PaymentPlan
        {
            InvoiceId = command.InvoiceId,
            StartsAtUtc = command.StartsAtUtc,
            Status = PaymentPlanStatus.Active,
            Terms = command.Terms.Trim()
        };

        foreach (var installment in installments)
        {
            plan.Installments.Add(installment);
        }

        dbContext.PaymentPlans.Add(plan);
        FinanceMath.ApplyPaymentPlanState(plan, invoice.Payments);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPaymentPlanAsync(plan.Id, asNoTracking: true, cancellationToken);
        return ToResult(saved!);
    }

    public async Task<PaymentPlanResult> UpdateAsync(Guid userId, UpdatePaymentPlanCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var plan = await LoadPaymentPlanAsync(command.PaymentPlanId, asNoTracking: false, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException("Payment plan was not found.");
        }

        if (plan.Installments.Any(entity => entity.Status == PaymentPlanInstallmentStatus.Paid))
        {
            throw new ValidationAppException("Payment plan installments with posted payments cannot be replaced.");
        }

        var installments = BuildInstallments(command.Installments);

        FinanceMath.ApplyInvoiceState(plan.Invoice!);
        var scheduledAmount = installments.Sum(entity => entity.Amount);
        if (!FinanceMath.AmountsMatch(scheduledAmount, plan.Invoice!.BalanceAmount))
        {
            throw new ValidationAppException("Payment plan installments must cover the invoice balance exactly.");
        }

        dbContext.PaymentPlanInstallments.RemoveRange(plan.Installments);
        plan.Installments.Clear();
        foreach (var installment in installments)
        {
            plan.Installments.Add(installment);
        }

        plan.StartsAtUtc = command.StartsAtUtc;
        plan.Terms = command.Terms.Trim();
        FinanceMath.ApplyPaymentPlanState(plan, plan.Invoice!.Payments);

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPaymentPlanAsync(plan.Id, asNoTracking: true, cancellationToken);
        return ToResult(saved!);
    }

    public async Task DeleteAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var plan = await LoadPaymentPlanAsync(paymentPlanId, asNoTracking: false, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException("Payment plan was not found.");
        }

        dbContext.PaymentPlanInstallments.RemoveRange(plan.Installments);
        dbContext.PaymentPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        await tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager);

        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);
    }

    private async Task<PaymentPlan?> LoadPaymentPlanAsync(Guid paymentPlanId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentPlans
            .Include(entity => entity.Installments)
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity!.Payments)
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity!.Lines)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(entity => entity.Id == paymentPlanId, cancellationToken);
    }

    private static List<PaymentPlanInstallment> BuildInstallments(IReadOnlyCollection<PaymentPlanInstallmentCommand> requests)
    {
        if (requests.Count == 0)
        {
            throw new ValidationAppException("At least one installment is required.");
        }

        var duplicateDueDates = requests
            .GroupBy(entity => entity.DueDateUtc)
            .Where(group => group.Count() > 1)
            .Any();
        if (duplicateDueDates)
        {
            throw new ValidationAppException("Installment due dates must be unique.");
        }

        return requests
            .OrderBy(entity => entity.DueDateUtc)
            .Select(entity => new PaymentPlanInstallment
            {
                DueDateUtc = entity.DueDateUtc,
                Amount = FinanceMath.RoundAmount(entity.Amount),
                Status = PaymentPlanInstallmentStatus.Scheduled
            })
            .ToList();
    }

    private static PaymentPlanResult ToResult(PaymentPlan entity)
    {
        var remainingAmount = entity.Invoice?.BalanceAmount ?? entity.Installments
            .Where(installment => installment.Status != PaymentPlanInstallmentStatus.Paid)
            .Sum(installment => installment.Amount);

        return InvoiceService.ToPaymentPlanResult(entity, remainingAmount);
    }
}
