using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.Bookings;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class BookingsController(ITrainingWorkflowService trainingWorkflowService) : ApiControllerBase
{
    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyCollection<BookingResponse>>> GetBookings(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetBookingsAsync(gymCode));
    }

    [HttpPost("bookings")]
    public async Task<ActionResult<BookingResponse>> CreateBooking(string gymCode, [FromBody] BookingCreateRequest request)
    {
        return Ok(await trainingWorkflowService.CreateBookingAsync(gymCode, request));
    }

    [HttpPut("bookings/{id:guid}/attendance")]
    public async Task<ActionResult<BookingResponse>> UpdateAttendance(string gymCode, Guid id, [FromBody] AttendanceUpdateRequest request)
    {
        return Ok(await trainingWorkflowService.UpdateAttendanceAsync(gymCode, id, request));
    }

    [HttpDelete("bookings/{id:guid}")]
    public async Task<ActionResult<Message>> CancelBooking(string gymCode, Guid id)
    {
        await trainingWorkflowService.CancelBookingAsync(gymCode, id);
        return Ok(new Message("Booking cancelled."));
}
}
