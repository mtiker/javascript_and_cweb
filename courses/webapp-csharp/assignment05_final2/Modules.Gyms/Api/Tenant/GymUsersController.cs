using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.GymUsers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Gyms.Api.Tenant;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/Tenant/GymUsersController.cs</c>
/// in Phase 5. Gym user-role management lives behind
/// <see cref="IMaintenanceWorkflowService"/> today (legacy bundling); the
/// route belongs to Gyms (tenant `{gymCode}/gym-users`). The workflow service
/// moves to Modules.Maintenance in Phase 8.
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
public class GymUsersController(IMaintenanceWorkflowService maintenanceWorkflowService) : ControllerBase
{
    [HttpGet("gym-users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GymUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GymUserResponse>>> GetGymUsers(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetGymUsersAsync(gymCode, cancellationToken));
    }

    [HttpPost("gym-users")]
    [ProducesResponseType(typeof(GymUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymUserResponse>> UpsertGymUser(string gymCode, [FromBody] GymUserUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpsertGymUserAsync(gymCode, request, cancellationToken));
    }

    [HttpDelete("gym-users/{appUserId:guid}/{roleName}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteGymUser(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteGymUserAsync(gymCode, appUserId, roleName, cancellationToken);
        return Ok(new Message("Gym user role deleted."));
    }
}
