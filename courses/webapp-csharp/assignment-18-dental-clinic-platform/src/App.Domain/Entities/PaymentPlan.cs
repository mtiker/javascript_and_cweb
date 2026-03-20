using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class PaymentPlan : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Active;
    public string Terms { get; set; } = default!;

    public Invoice? Invoice { get; set; }
    public ICollection<PaymentPlanInstallment> Installments { get; set; } = new List<PaymentPlanInstallment>();
}
