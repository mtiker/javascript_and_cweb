using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.CompanyUsers;

public class UpsertCompanyUserRequest
{
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string RoleName { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    [MinLength(8)]
    [MaxLength(100)]
    public string? TemporaryPassword { get; set; }
}
