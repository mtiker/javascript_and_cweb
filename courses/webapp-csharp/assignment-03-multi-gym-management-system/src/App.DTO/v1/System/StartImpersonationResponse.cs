namespace App.DTO.v1.System;

public class StartImpersonationResponse
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }

    public Guid UserId { get; set; }
    public Guid TargetUserId { get; set; }
    public Guid ImpersonatedByUserId { get; set; }
    public string ImpersonationReason { get; set; } = default!;

    public Guid? ActiveGymId { get; set; }
    public string? GymCode { get; set; }
    public string? ActiveRole { get; set; }
}
