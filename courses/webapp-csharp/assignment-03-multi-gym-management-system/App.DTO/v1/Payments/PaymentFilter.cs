using App.Domain.Enums;

namespace App.DTO.v1.Payments;

public class PaymentFilter
{
    public PaymentStatus? Status { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
