using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Dentists;

public class CreateDentistRequest
{
    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string LicenseNumber { get; set; } = default!;

    [MaxLength(128)]
    public string? Specialty { get; set; }
}
