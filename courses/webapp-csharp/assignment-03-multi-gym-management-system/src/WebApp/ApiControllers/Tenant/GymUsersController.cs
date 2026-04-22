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
    public async Task<ActionResult<IReadOnlyCollection<GymUserResponse>>> GetGymUsers(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetGymUsersAsync(gymCode));
    }

    [HttpPost("gym-users")]
    public async Task<ActionResult<GymUserResponse>> UpsertGymUser(string gymCode, [FromBody] GymUserUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpsertGymUserAsync(gymCode, request));
    }

    [HttpDelete("gym-users/{appUserId:guid}/{roleName}")]
    public async Task<ActionResult<Message>> DeleteGymUser(string gymCode, Guid appUserId, string roleName)
    {
        await maintenanceWorkflowService.DeleteGymUserAsync(gymCode, appUserId, roleName);
        return Ok(new Message("Gym user role deleted."));
    }
}
