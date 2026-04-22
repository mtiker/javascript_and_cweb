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
    [ProducesResponseType(typeof(IReadOnlyCollection<VacationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VacationResponse>>> GetVacations(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.GetVacationsAsync(gymCode, cancellationToken));
    }

    [HttpPost("vacations")]
    [ProducesResponseType(typeof(VacationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VacationResponse>> CreateVacation(string gymCode, [FromBody] VacationUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.CreateVacationAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("vacations/{id:guid}")]
    [ProducesResponseType(typeof(VacationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VacationResponse>> UpdateVacation(string gymCode, Guid id, [FromBody] VacationUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.UpdateVacationAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("vacations/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteVacation(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await staffWorkflowService.DeleteVacationAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Vacation deleted."));
    }
}
