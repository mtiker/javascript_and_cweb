using App.Domain.Enums;

namespace App.DTO.v1.System;

public class StartImpersonationResponse
{
    public string Jwt { get; set; } = default!;
    public Guid UserId { get; set; }
    public string? GymCode { get; set; }
}
