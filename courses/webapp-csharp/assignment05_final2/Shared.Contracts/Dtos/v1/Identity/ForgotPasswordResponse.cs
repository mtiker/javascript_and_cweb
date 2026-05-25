namespace Shared.Contracts.Dtos.v1.Identity;

public class ForgotPasswordResponse
{
    public string Message { get; set; } = default!;
    public string? ResetToken { get; set; }
}
