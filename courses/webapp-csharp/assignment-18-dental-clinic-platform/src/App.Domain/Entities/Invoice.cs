using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Invoice : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? CostEstimateId { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateTime DueDateUtc { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public Patient? Patient { get; set; }
    public CostEstimate? CostEstimate { get; set; }
    public PaymentPlan? PaymentPlan { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
