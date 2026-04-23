namespace App.DTO.v1.System;

public class StartImpersonationRequest
{
    public Guid UserId { get; set; }
    public string? GymCode { get; set; }
    public string? Reason { get; set; }
}
