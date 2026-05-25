using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.GymSettings;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Gyms.Api.Tenant;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/Tenant/GymSettingsController.cs</c>
/// in Phase 5. The settings persistence lives behind
/// <see cref="IMaintenanceWorkflowService"/> today; the route belongs to
/// Gyms (tenant `{gymCode}/gym-settings`). Phase 8 (Maintenance extraction)
/// will move the workflow service and may route this through an inter-module
/// contract instead of the direct App.BLL.Contracts reference.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/{gymCode}")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class GymSettingsController(IMaintenanceWorkflowService maintenanceWorkflowService) : ControllerBase
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
