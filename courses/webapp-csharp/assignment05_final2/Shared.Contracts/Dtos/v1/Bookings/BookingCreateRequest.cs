using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Bookings;

public class BookingCreateRequest
{
    public Guid TrainingSessionId { get; set; }
    public Guid MemberId { get; set; }
    public string? PaymentReference { get; set; }
}
