using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Memberships;

public class SellMembershipRequest
{
    public Guid MemberId { get; set; }
    public Guid MembershipPackageId { get; set; }
    public DateOnly? RequestedStartDate { get; set; }
    public string? PaymentReference { get; set; }
}
