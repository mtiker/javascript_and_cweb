using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class CostEstimate : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid? InsurancePlanId { get; set; }
    public Guid? PatientInsurancePolicyId { get; set; }
    public string EstimateNumber { get; set; } = default!;
    public string FormatCode { get; set; } = default!;
    public decimal TotalEstimatedAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal PatientEstimatedAmount { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public CostEstimateStatus Status { get; set; } = CostEstimateStatus.Draft;

    public Patient? Patient { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }
    public PatientInsurancePolicy? PatientInsurancePolicy { get; set; }
}
