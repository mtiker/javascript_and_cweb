using App.Domain.Enums;

namespace App.DTO.v1.System;

public class StartImpersonationRequest
{
    public Guid UserId { get; set; }
    public string? GymCode { get; set; }
}
