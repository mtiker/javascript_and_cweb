using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class ResetPasswordRequest
{
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(10)]
    public string ResetToken { get; set; } = default!;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = default!;
}
