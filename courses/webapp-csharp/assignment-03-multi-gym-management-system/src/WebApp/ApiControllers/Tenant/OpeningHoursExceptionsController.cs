using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.OpeningHoursExceptions;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class OpeningHoursExceptionsController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
    [HttpGet("opening-hours-exceptions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OpeningHoursExceptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursExceptionResponse>>> GetOpeningHourExceptions(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHourExceptionsAsync(gymCode, cancellationToken));
    }

    [HttpPost("opening-hours-exceptions")]
    [ProducesResponseType(typeof(OpeningHoursExceptionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> CreateOpeningHourException(string gymCode, [FromBody] OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHourExceptionAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("opening-hours-exceptions/{id:guid}")]
    [ProducesResponseType(typeof(OpeningHoursExceptionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> UpdateOpeningHourException(string gymCode, Guid id, [FromBody] OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHourExceptionAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("opening-hours-exceptions/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteOpeningHourException(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteOpeningHourExceptionAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Opening hours exception deleted."));
}
}
