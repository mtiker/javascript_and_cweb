using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class TreatmentPlan : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? DentistId { get; set; }
    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;
    public DateTime? ApprovedAtUtc { get; set; }

    public Patient? Patient { get; set; }
    public Dentist? Dentist { get; set; }
    public ICollection<PlanItem> Items { get; set; } = new List<PlanItem>();
}
