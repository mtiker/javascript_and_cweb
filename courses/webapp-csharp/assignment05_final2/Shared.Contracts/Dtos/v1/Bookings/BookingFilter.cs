using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Bookings;

public class BookingFilter
{
    public BookingStatus? Status { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? TrainingSessionId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
