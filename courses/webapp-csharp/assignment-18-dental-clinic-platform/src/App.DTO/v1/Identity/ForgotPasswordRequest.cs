using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class ForgotPasswordRequest
{
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = default!;
}
