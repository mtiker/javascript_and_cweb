using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.GymUsers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class GymUsersController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
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
