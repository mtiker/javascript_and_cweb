using App.BLL.Contracts.Services;
using App.Domain.Enums;
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
    public async Task<ActionResult<IReadOnlyCollection<BookingResponse>>> GetBookings(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] BookingStatus? status = null,
        [FromQuery] Guid? memberId = null,
        [FromQuery] Guid? trainingSessionId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var filter = new BookingFilter
        {
            Status = status,
            MemberId = memberId,
            TrainingSessionId = trainingSessionId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        return Ok(await trainingWorkflowService.GetBookingsAsync(gymCode, filter, cancellationToken));
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

    [HttpPut("bookings/{id:guid}/reschedule")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingResponse>> RescheduleBooking(string gymCode, Guid id, [FromBody] BookingRescheduleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.RescheduleBookingAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("bookings/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelBooking(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.CancelBookingAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
