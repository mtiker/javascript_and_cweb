namespace App.DTO.v1.System;

public class StartImpersonationResponse
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
    public Guid ActiveCompanyId { get; set; }
    public string ActiveCompanySlug { get; set; } = default!;
    public string ActiveCompanyRole { get; set; } = default!;
    public Guid ImpersonatedByUserId { get; set; }
    public string ImpersonationReason { get; set; } = default!;
    public Guid TargetUserId { get; set; }
    public string TargetUserEmail { get; set; } = default!;
}
