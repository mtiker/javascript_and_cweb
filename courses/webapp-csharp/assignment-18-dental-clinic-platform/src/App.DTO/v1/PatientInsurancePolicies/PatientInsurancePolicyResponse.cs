namespace App.DTO.v1.PatientInsurancePolicies;

public class PatientInsurancePolicyResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid InsurancePlanId { get; set; }
    public string InsurancePlanName { get; set; } = default!;
    public string PolicyNumber { get; set; } = default!;
    public string? MemberNumber { get; set; }
    public string? GroupNumber { get; set; }
    public DateOnly CoverageStart { get; set; }
    public DateOnly? CoverageEnd { get; set; }
    public decimal AnnualMaximum { get; set; }
    public decimal Deductible { get; set; }
    public decimal CoveragePercent { get; set; }
    public string Status { get; set; } = default!;
}
