using App.BLL.Contracts;
using App.DTO.v1;
using App.DTO.v1.Tenant;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class TrainingController(ITrainingWorkflowService trainingWorkflowService) : ApiControllerBase
{
    [HttpGet("training-categories")]
    public async Task<ActionResult<IReadOnlyCollection<TrainingCategoryResponse>>> GetCategories(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetCategoriesAsync(gymCode));
    }

    [HttpPost("training-categories")]
    public async Task<ActionResult<TrainingCategoryResponse>> CreateCategory(string gymCode, [FromBody] TrainingCategoryUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.CreateCategoryAsync(gymCode, request));
    }

    [HttpPut("training-categories/{id:guid}")]
    public async Task<ActionResult<TrainingCategoryResponse>> UpdateCategory(string gymCode, Guid id, [FromBody] TrainingCategoryUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpdateCategoryAsync(gymCode, id, request));
    }

    [HttpDelete("training-categories/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteCategory(string gymCode, Guid id)
    {
        await trainingWorkflowService.DeleteCategoryAsync(gymCode, id);
        return Ok(new Message("Training category deleted."));
    }

    [HttpGet("training-sessions")]
    public async Task<ActionResult<IReadOnlyCollection<TrainingSessionResponse>>> GetSessions(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetSessionsAsync(gymCode));
    }

    [HttpGet("training-sessions/{id:guid}")]
    public async Task<ActionResult<TrainingSessionResponse>> GetSession(string gymCode, Guid id)
    {
        return Ok(await trainingWorkflowService.GetSessionAsync(gymCode, id));
    }

    [HttpPost("training-sessions")]
    public async Task<ActionResult<TrainingSessionResponse>> CreateSession(string gymCode, [FromBody] TrainingSessionUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, null, request));
    }

    [HttpPut("training-sessions/{id:guid}")]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSession(string gymCode, Guid id, [FromBody] TrainingSessionUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, id, request));
    }

    [HttpDelete("training-sessions/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteSession(string gymCode, Guid id)
    {
        await trainingWorkflowService.DeleteSessionAsync(gymCode, id);
        return Ok(new Message("Training session deleted."));
    }

    [HttpGet("work-shifts")]
    public async Task<ActionResult<IReadOnlyCollection<WorkShiftResponse>>> GetWorkShifts(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetWorkShiftsAsync(gymCode));
    }

    [HttpPost("work-shifts")]
    public async Task<ActionResult<WorkShiftResponse>> CreateWorkShift(string gymCode, [FromBody] WorkShiftUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.CreateWorkShiftAsync(gymCode, request));
    }

    [HttpPut("work-shifts/{id:guid}")]
    public async Task<ActionResult<WorkShiftResponse>> UpdateWorkShift(string gymCode, Guid id, [FromBody] WorkShiftUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpdateWorkShiftAsync(gymCode, id, request));
    }

    [HttpDelete("work-shifts/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteWorkShift(string gymCode, Guid id)
    {
        await trainingWorkflowService.DeleteWorkShiftAsync(gymCode, id);
        return Ok(new Message("Work shift deleted."));
    }

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
