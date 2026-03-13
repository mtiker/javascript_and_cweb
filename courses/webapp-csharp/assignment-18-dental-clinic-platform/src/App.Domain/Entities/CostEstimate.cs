using App.Domain.Common;

namespace App.Domain.Entities;

public class CostEstimate : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid? InsurancePlanId { get; set; }
    public string EstimateNumber { get; set; } = default!;
    public string FormatCode { get; set; } = default!;
    public decimal TotalEstimatedAmount { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Draft";

    public Patient? Patient { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }
}
