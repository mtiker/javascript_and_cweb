using App.Domain.Enums;

namespace App.DTO.v1.Memberships;

public class MembershipResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid MembershipPackageId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public MembershipStatus Status { get; set; }
}
