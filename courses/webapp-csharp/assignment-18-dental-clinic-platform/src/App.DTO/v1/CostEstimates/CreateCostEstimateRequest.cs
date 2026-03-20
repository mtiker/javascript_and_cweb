using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.CostEstimates;

public class CreateCostEstimateRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid TreatmentPlanId { get; set; }

    public Guid? PatientInsurancePolicyId { get; set; }

    [Required]
    [MaxLength(64)]
    public string EstimateNumber { get; set; } = default!;

    [Required]
    [MaxLength(32)]
    public string FormatCode { get; set; } = default!;
}
