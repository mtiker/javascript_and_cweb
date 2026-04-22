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
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursExceptionResponse>>> GetOpeningHourExceptions(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHourExceptionsAsync(gymCode));
    }

    [HttpPost("opening-hours-exceptions")]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> CreateOpeningHourException(string gymCode, [FromBody] OpeningHoursExceptionUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHourExceptionAsync(gymCode, request));
    }

    [HttpPut("opening-hours-exceptions/{id:guid}")]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> UpdateOpeningHourException(string gymCode, Guid id, [FromBody] OpeningHoursExceptionUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHourExceptionAsync(gymCode, id, request));
    }

    [HttpDelete("opening-hours-exceptions/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteOpeningHourException(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteOpeningHourExceptionAsync(gymCode, id);
        return Ok(new Message("Opening hours exception deleted."));
}
}
