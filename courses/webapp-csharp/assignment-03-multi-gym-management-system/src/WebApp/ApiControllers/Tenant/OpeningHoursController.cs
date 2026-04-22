using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.OpeningHours;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class OpeningHoursController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
    [HttpGet("opening-hours")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OpeningHoursResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursResponse>>> GetOpeningHours(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHoursAsync(gymCode, cancellationToken));
    }

    [HttpPost("opening-hours")]
    [ProducesResponseType(typeof(OpeningHoursResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpeningHoursResponse>> CreateOpeningHours(string gymCode, [FromBody] OpeningHoursUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHoursAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("opening-hours/{id:guid}")]
    [ProducesResponseType(typeof(OpeningHoursResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpeningHoursResponse>> UpdateOpeningHours(string gymCode, Guid id, [FromBody] OpeningHoursUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHoursAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("opening-hours/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteOpeningHours(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteOpeningHoursAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Opening hours deleted."));
}
}
