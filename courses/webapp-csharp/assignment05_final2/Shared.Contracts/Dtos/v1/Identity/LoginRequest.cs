namespace Shared.Contracts.Dtos.v1.Identity;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
