using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.WorkShifts;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class WorkShiftsController(ITrainingWorkflowService trainingWorkflowService) : ApiControllerBase
{
    [HttpGet("work-shifts")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkShiftResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<WorkShiftResponse>>> GetWorkShifts(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetWorkShiftsAsync(gymCode, cancellationToken));
    }

    [HttpPost("work-shifts")]
    [ProducesResponseType(typeof(WorkShiftResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkShiftResponse>> CreateWorkShift(string gymCode, [FromBody] WorkShiftUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.CreateWorkShiftAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("work-shifts/{id:guid}")]
    [ProducesResponseType(typeof(WorkShiftResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkShiftResponse>> UpdateWorkShift(string gymCode, Guid id, [FromBody] WorkShiftUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpdateWorkShiftAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("work-shifts/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteWorkShift(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.DeleteWorkShiftAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Work shift deleted."));
    }
}
