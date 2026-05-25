using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Payments;

public class PaymentFilter
{
    public PaymentStatus? Status { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
