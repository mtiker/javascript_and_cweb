namespace App.DTO.v1.Identity;

public class RefreshTokenRequest
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
