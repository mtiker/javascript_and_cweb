namespace App.DTO.v1.CostEstimates;

public class CostEstimateResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid? PatientInsurancePolicyId { get; set; }
    public Guid? InsurancePlanId { get; set; }
    public string EstimateNumber { get; set; } = default!;
    public string FormatCode { get; set; } = default!;
    public decimal TotalEstimatedAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal PatientEstimatedAmount { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string Status { get; set; } = default!;
}
