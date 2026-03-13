using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.InsurancePlans;

public class UpdateInsurancePlanRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [Required]
    [MinLength(2)]
    [MaxLength(2)]
    public string CountryCode { get; set; } = default!;

    [Required]
    [MaxLength(32)]
    public string CoverageType { get; set; } = default!;

    public bool IsActivePlan { get; set; } = true;

    [MaxLength(256)]
    public string? ClaimSubmissionEndpoint { get; set; }
}
