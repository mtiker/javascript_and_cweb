using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System;

public class StartImpersonationRequest
{
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string TargetUserEmail { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string CompanySlug { get; set; } = default!;

    [Required]
    [MinLength(8)]
    [MaxLength(512)]
    public string Reason { get; set; } = default!;
}
