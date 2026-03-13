using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System;

public class RegisterCompanyRequest
{
    [Required]
    [MaxLength(128)]
    public string CompanyName { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    [RegularExpression("^[a-z0-9-]+$")]
    public string CompanySlug { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string OwnerEmail { get; set; } = default!;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string OwnerPassword { get; set; } = default!;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "DE";
}
