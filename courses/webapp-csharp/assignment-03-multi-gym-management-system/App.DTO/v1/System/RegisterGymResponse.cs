using App.Domain.Enums;

namespace App.DTO.v1.System;

public class RegisterGymResponse
{
    public Guid GymId { get; set; }
    public string GymCode { get; set; } = default!;
    public Guid OwnerUserId { get; set; }
}
