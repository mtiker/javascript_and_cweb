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
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursResponse>>> GetOpeningHours(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHoursAsync(gymCode));
    }

    [HttpPost("opening-hours")]
    public async Task<ActionResult<OpeningHoursResponse>> CreateOpeningHours(string gymCode, [FromBody] OpeningHoursUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHoursAsync(gymCode, request));
    }

    [HttpPut("opening-hours/{id:guid}")]
    public async Task<ActionResult<OpeningHoursResponse>> UpdateOpeningHours(string gymCode, Guid id, [FromBody] OpeningHoursUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHoursAsync(gymCode, id, request));
    }

    [HttpDelete("opening-hours/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteOpeningHours(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteOpeningHoursAsync(gymCode, id);
        return Ok(new Message("Opening hours deleted."));
}
}
