using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Bookings;

public class AttendanceUpdateRequest
{
    public BookingStatus Status { get; set; }
}
