namespace App.DTO.v1.Identity;

public class JwtResponse
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
    public Guid? ActiveGymId { get; set; }
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public string[] SystemRoles { get; set; } = [];
    public TenantAccessResponse[] AvailableTenants { get; set; } = [];
}

public class TenantAccessResponse
{
    public Guid GymId { get; set; }
    public string GymCode { get; set; } = default!;
    public string GymName { get; set; } = default!;
    public string[] Roles { get; set; } = [];
}
