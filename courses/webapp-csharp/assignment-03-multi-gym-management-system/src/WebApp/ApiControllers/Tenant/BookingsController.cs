using App.BLL.Services;
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
    [ProducesResponseType(typeof(IReadOnlyCollection<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<BookingResponse>>> GetBookings(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetBookingsAsync(gymCode, cancellationToken));
    }

    [HttpPost("bookings")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BookingResponse>> CreateBooking(string gymCode, [FromBody] BookingCreateRequest request, CancellationToken cancellationToken)
    {
        var created = await trainingWorkflowService.CreateBookingAsync(gymCode, request, cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPut("bookings/{id:guid}/attendance")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingResponse>> UpdateAttendance(string gymCode, Guid id, [FromBody] AttendanceUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpdateAttendanceAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("bookings/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelBooking(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.CancelBookingAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
