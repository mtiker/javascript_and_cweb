using Modules.Gyms.Application;
using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.GymUsers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Gyms.Api.Tenant;

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
public class GymUsersController(IGymsTenantWorkflowService workflowService) : ControllerBase
{
    [HttpGet("gym-users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GymUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GymUserResponse>>> GetGymUsers(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetGymUsersAsync(gymCode, cancellationToken));
    }

    [HttpPost("gym-users")]
    [ProducesResponseType(typeof(GymUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GymUserResponse>> UpsertGymUser(string gymCode, [FromBody] GymUserUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.UpsertGymUserAsync(gymCode, request, cancellationToken));
    }

    [HttpDelete("gym-users/{appUserId:guid}/{roleName}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteGymUser(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken)
    {
        await workflowService.DeleteGymUserAsync(gymCode, appUserId, roleName, cancellationToken);
        return Ok(new Message("Gym user role deleted."));
    }
}
