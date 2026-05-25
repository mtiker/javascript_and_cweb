namespace Shared.Contracts.Dtos.v1.Identity;

public class RefreshTokenRequest
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
