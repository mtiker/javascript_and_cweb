using App.Domain.Enums;

namespace App.DTO.v1.Memberships;

public class SellMembershipRequest
{
    public Guid MemberId { get; set; }
    public Guid MembershipPackageId { get; set; }
    public DateOnly? RequestedStartDate { get; set; }
    public string? PaymentReference { get; set; }
}
