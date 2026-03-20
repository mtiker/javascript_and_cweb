using App.Domain.Common;

namespace App.Domain.Entities;

public class InvoiceLine : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid? TreatmentId { get; set; }
    public Guid? PlanItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal PatientAmount { get; set; }

    public Invoice? Invoice { get; set; }
    public Treatment? Treatment { get; set; }
    public PlanItem? PlanItem { get; set; }
}
