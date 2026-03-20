using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class PaymentPlanInstallment : TenantBaseEntity
{
    public Guid PaymentPlanId { get; set; }
    public DateTime DueDateUtc { get; set; }
    public decimal Amount { get; set; }
    public PaymentPlanInstallmentStatus Status { get; set; } = PaymentPlanInstallmentStatus.Scheduled;
    public DateTime? PaidAtUtc { get; set; }

    public PaymentPlan? PaymentPlan { get; set; }
}
