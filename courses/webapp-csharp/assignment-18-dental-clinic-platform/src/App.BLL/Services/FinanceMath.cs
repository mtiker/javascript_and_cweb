using App.Domain.Entities;
using App.Domain.Enums;

namespace App.BLL.Services;

public static class FinanceMath
{
    public static EstimateBreakdown CalculateEstimate(decimal totalAmount, PatientInsurancePolicy? policy)
    {
        var roundedTotal = RoundAmount(totalAmount);
        if (policy == null)
        {
            return new EstimateBreakdown(roundedTotal, 0m, roundedTotal);
        }

        var deductible = Math.Max(0m, policy.Deductible);
        var coveragePercent = Math.Clamp(policy.CoveragePercent, 0m, 100m);
        var annualMaximum = Math.Max(0m, policy.AnnualMaximum);

        var eligibleAmount = Math.Max(0m, roundedTotal - deductible);
        var coverageAmount = RoundAmount(eligibleAmount * (coveragePercent / 100m));
        if (annualMaximum > 0m)
        {
            coverageAmount = Math.Min(coverageAmount, annualMaximum);
        }

        coverageAmount = Math.Min(coverageAmount, roundedTotal);
        var patientAmount = RoundAmount(roundedTotal - coverageAmount);

        return new EstimateBreakdown(roundedTotal, coverageAmount, patientAmount);
    }

    public static void NormalizeInvoiceLine(InvoiceLine line)
    {
        line.LineTotal = RoundAmount(line.Quantity * line.UnitPrice);
        line.CoverageAmount = RoundAmount(Math.Min(line.CoverageAmount, line.LineTotal));
        line.PatientAmount = RoundAmount(line.LineTotal - line.CoverageAmount);
    }

    public static void ApplyInvoiceState(Invoice invoice, DateTime? nowUtc = null)
    {
        invoice.TotalAmount = RoundAmount(invoice.Lines.Sum(entity => entity.LineTotal));
        var patientAmount = RoundAmount(invoice.Lines.Sum(entity => entity.PatientAmount));
        var paidAmount = RoundAmount(invoice.Payments.Sum(entity => entity.Amount));
        invoice.BalanceAmount = RoundAmount(Math.Max(0m, patientAmount - paidAmount));

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        var now = nowUtc ?? DateTime.UtcNow;
        invoice.Status = invoice.BalanceAmount <= 0m
            ? InvoiceStatus.Paid
            : invoice.DueDateUtc < now
                ? InvoiceStatus.Overdue
                : InvoiceStatus.Issued;
    }

    public static void ApplyPaymentPlanState(PaymentPlan plan, IEnumerable<Payment> payments, DateTime? nowUtc = null)
    {
        var installments = plan.Installments
            .OrderBy(entity => entity.DueDateUtc)
            .ThenBy(entity => entity.CreatedAtUtc)
            .ToList();

        var paymentList = payments
            .OrderBy(entity => entity.PaidAtUtc)
            .ToList();

        if (plan.Status == PaymentPlanStatus.Cancelled)
        {
            foreach (var installment in installments.Where(entity => entity.Status != PaymentPlanInstallmentStatus.Paid))
            {
                installment.Status = PaymentPlanInstallmentStatus.Cancelled;
                installment.PaidAtUtc = null;
            }

            return;
        }

        var remainingPaidAmount = RoundAmount(paymentList.Sum(entity => entity.Amount));
        var lastPaymentDate = paymentList.LastOrDefault()?.PaidAtUtc;
        var now = nowUtc ?? DateTime.UtcNow;

        foreach (var installment in installments)
        {
            if (remainingPaidAmount >= installment.Amount)
            {
                installment.Status = PaymentPlanInstallmentStatus.Paid;
                installment.PaidAtUtc ??= lastPaymentDate;
                remainingPaidAmount = RoundAmount(remainingPaidAmount - installment.Amount);
                continue;
            }

            installment.PaidAtUtc = null;
            installment.Status = installment.DueDateUtc < now
                ? PaymentPlanInstallmentStatus.Overdue
                : PaymentPlanInstallmentStatus.Scheduled;
        }

        plan.Status = installments.Count == 0
            ? PaymentPlanStatus.Active
            : installments.All(entity => entity.Status == PaymentPlanInstallmentStatus.Paid)
                ? PaymentPlanStatus.Completed
                : installments.Any(entity => entity.Status == PaymentPlanInstallmentStatus.Overdue)
                    ? PaymentPlanStatus.Defaulted
                    : PaymentPlanStatus.Active;
    }

    public static decimal RoundAmount(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static bool AmountsMatch(decimal left, decimal right)
    {
        return Math.Abs(left - right) < 0.01m;
    }
}

public readonly record struct EstimateBreakdown(decimal TotalAmount, decimal CoverageAmount, decimal PatientAmount);
