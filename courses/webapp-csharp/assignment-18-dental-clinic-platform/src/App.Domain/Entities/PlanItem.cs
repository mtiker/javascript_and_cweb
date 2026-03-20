using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class PlanItem : TenantBaseEntity
{
    public Guid TreatmentPlanId { get; set; }
    public Guid TreatmentTypeId { get; set; }
    public int Sequence { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public decimal EstimatedPrice { get; set; }
    public PlanItemDecision Decision { get; set; } = PlanItemDecision.Pending;
    public DateTime? DecisionAtUtc { get; set; }
    public string? DecisionNotes { get; set; }

    public TreatmentPlan? TreatmentPlan { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
