using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.GymManagement.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.GymSettings;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class GymSettingsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("gym-settings")]
    [ProducesResponseType(typeof(GymSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymSettingsResponse>> GetGymSettings(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GetGymSettingsQuery(gymCode), cancellationToken));
    }

    [HttpPut("gym-settings")]
    [ProducesResponseType(typeof(GymSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymSettingsResponse>> UpdateGymSettings(string gymCode, [FromBody] GymSettingsUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateGymSettingsCommand(gymCode, request), cancellationToken));
    }
}
