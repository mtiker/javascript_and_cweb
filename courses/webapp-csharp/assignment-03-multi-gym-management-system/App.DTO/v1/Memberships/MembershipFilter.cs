using App.Domain.Enums;

namespace App.DTO.v1.Memberships;

public class MembershipFilter
{
    public MembershipStatus? Status { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? MembershipPackageId { get; set; }
    public DateOnly? StartFrom { get; set; }
    public DateOnly? StartTo { get; set; }
}
