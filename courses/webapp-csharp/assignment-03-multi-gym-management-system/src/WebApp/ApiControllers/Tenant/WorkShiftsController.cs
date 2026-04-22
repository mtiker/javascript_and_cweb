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
}
