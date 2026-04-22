using App.Domain.Enums;

namespace App.DTO.v1.GymUsers;

public class GymUserUpsertRequest
{
    public Guid AppUserId { get; set; }
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
