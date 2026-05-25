using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Memberships;

public class MembershipStatusUpdateRequest
{
    public MembershipStatus Status { get; set; }
    public string? Reason { get; set; }
}
