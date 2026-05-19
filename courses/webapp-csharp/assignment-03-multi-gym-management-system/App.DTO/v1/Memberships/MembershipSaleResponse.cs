using App.Domain.Enums;

namespace App.DTO.v1.Memberships;

public class MembershipSaleResponse
{
    public Guid MembershipId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool OverlapDetected { get; set; }
    public DateOnly? SuggestedStartDate { get; set; }
}
