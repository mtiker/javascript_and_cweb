using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class PatientInsurancePolicy : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid InsurancePlanId { get; set; }
    public string PolicyNumber { get; set; } = default!;
    public string? MemberNumber { get; set; }
    public string? GroupNumber { get; set; }
    public DateOnly CoverageStart { get; set; }
    public DateOnly? CoverageEnd { get; set; }
    public decimal AnnualMaximum { get; set; }
    public decimal Deductible { get; set; }
    public decimal CoveragePercent { get; set; }
    public PatientInsurancePolicyStatus Status { get; set; } = PatientInsurancePolicyStatus.Active;

    public Patient? Patient { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }
    public ICollection<CostEstimate> CostEstimates { get; set; } = new List<CostEstimate>();
}
