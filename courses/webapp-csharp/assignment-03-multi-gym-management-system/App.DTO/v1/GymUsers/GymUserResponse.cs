using App.Domain.Enums;

namespace App.DTO.v1.GymUsers;

public class GymUserResponse
{
    public Guid AppUserId { get; set; }
    public string Email { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; }
}
