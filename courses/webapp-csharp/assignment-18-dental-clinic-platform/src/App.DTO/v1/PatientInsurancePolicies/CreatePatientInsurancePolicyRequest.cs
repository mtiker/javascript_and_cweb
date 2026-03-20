using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.PatientInsurancePolicies;

public class CreatePatientInsurancePolicyRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid InsurancePlanId { get; set; }

    [Required]
    [MaxLength(64)]
    public string PolicyNumber { get; set; } = default!;

    [MaxLength(64)]
    public string? MemberNumber { get; set; }

    [MaxLength(64)]
    public string? GroupNumber { get; set; }

    [Required]
    public DateOnly CoverageStart { get; set; }

    public DateOnly? CoverageEnd { get; set; }

    [Range(0, 999999999)]
    public decimal AnnualMaximum { get; set; }

    [Range(0, 999999999)]
    public decimal Deductible { get; set; }

    [Range(0, 100)]
    public decimal CoveragePercent { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = default!;
}
