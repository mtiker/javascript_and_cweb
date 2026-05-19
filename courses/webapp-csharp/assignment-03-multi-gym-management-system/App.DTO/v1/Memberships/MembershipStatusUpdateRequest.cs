using App.Domain.Enums;

namespace App.DTO.v1.Memberships;

public class MembershipStatusUpdateRequest
{
    public MembershipStatus Status { get; set; }
    public string? Reason { get; set; }
}
