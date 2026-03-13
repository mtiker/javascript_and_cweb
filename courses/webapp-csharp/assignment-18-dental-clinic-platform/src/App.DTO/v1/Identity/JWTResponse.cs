namespace App.DTO.v1.Identity;

public class JWTResponse
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
    public Guid? ActiveCompanyId { get; set; }
    public string? ActiveCompanySlug { get; set; }
    public string? ActiveCompanyRole { get; set; }
}
