using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.Vacations;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class VacationsController(IStaffWorkflowService staffWorkflowService) : ApiControllerBase
{
    [HttpGet("vacations")]
    public async Task<ActionResult<IReadOnlyCollection<VacationResponse>>> GetVacations(string gymCode)
    {
        return Ok(await staffWorkflowService.GetVacationsAsync(gymCode));
    }

    [HttpPost("vacations")]
    public async Task<ActionResult<VacationResponse>> CreateVacation(string gymCode, [FromBody] VacationUpsertRequest request)
    {
        return Ok(await staffWorkflowService.CreateVacationAsync(gymCode, request));
    }

    [HttpPut("vacations/{id:guid}")]
    public async Task<ActionResult<VacationResponse>> UpdateVacation(string gymCode, Guid id, [FromBody] VacationUpsertRequest request)
    {
        return Ok(await staffWorkflowService.UpdateVacationAsync(gymCode, id, request));
    }

    [HttpDelete("vacations/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteVacation(string gymCode, Guid id)
    {
        await staffWorkflowService.DeleteVacationAsync(gymCode, id);
        return Ok(new Message("Vacation deleted."));
    }
}
