using App.Domain.Enums;

namespace App.DTO.v1.Bookings;

public class AttendanceUpdateRequest
{
    public BookingStatus Status { get; set; }
}
