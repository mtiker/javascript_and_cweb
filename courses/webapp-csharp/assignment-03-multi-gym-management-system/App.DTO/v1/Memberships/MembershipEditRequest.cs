namespace App.DTO.v1.Memberships;

public class MembershipEditRequest
{
    public Guid MembershipPackageId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
