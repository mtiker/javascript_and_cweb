using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class SwitchCompanyRequest
{
    [Required]
    [MaxLength(64)]
    public string CompanySlug { get; set; } = default!;
}
