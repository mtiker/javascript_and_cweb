using App.Domain.Enums;

namespace App.DTO.v1.Bookings;

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid TrainingSessionId { get; set; }
    public string TrainingSessionName { get; set; } = default!;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public string MemberCode { get; set; } = default!;
    public BookingStatus Status { get; set; }
    public decimal ChargedPrice { get; set; }
    public bool PaymentRequired { get; set; }
}
