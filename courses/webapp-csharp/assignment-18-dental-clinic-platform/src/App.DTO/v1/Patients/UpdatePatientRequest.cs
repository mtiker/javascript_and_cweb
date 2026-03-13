using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Patients;

public class UpdatePatientRequest
{
    [Required]
    [MaxLength(64)]
    public string FirstName { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string LastName { get; set; } = default!;

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(64)]
    public string? PersonalCode { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }
}
