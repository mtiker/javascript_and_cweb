using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.GymSettings;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class GymSettingsController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
    [HttpGet("gym-settings")]
    [ProducesResponseType(typeof(GymSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymSettingsResponse>> GetGymSettings(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetGymSettingsAsync(gymCode, cancellationToken));
    }

    [HttpPut("gym-settings")]
    [ProducesResponseType(typeof(GymSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymSettingsResponse>> UpdateGymSettings(string gymCode, [FromBody] GymSettingsUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateGymSettingsAsync(gymCode, request, cancellationToken));
}
}
