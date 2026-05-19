namespace App.DTO.v1.Identity;

public class ResetPasswordRequest
{
    public string Email { get; set; } = default!;
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
