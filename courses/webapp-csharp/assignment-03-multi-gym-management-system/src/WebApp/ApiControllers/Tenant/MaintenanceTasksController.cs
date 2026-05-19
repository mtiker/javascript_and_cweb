using App.DTO.v1;
using App.DTO.v1.MaintenanceTasks;
using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.GymManagement.Contracts;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MaintenanceTasksController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("maintenance-tasks")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MaintenanceTaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MaintenanceTaskResponse>>> GetMaintenanceTasks(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListMaintenanceTasksQuery(gymCode), cancellationToken));
    }

    [HttpPost("maintenance-tasks")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MaintenanceTaskResponse>> CreateMaintenanceTask(string gymCode, [FromBody] MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.SendAsync(new CreateMaintenanceTaskCommand(gymCode, request), cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPut("maintenance-tasks/{id:guid}/status")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskStatus(string gymCode, Guid id, [FromBody] MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateMaintenanceTaskStatusCommand(gymCode, id, request), cancellationToken));
    }

    [HttpPut("maintenance-tasks/{id:guid}/assignment")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskAssignment(string gymCode, Guid id, [FromBody] MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateMaintenanceTaskAssignmentCommand(gymCode, id, request), cancellationToken));
    }

    [HttpPost("maintenance-tasks/generate-due")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> GenerateDueTasks(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GenerateDueMaintenanceTasksCommand(gymCode), cancellationToken));
    }

    [HttpDelete("maintenance-tasks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMaintenanceTask(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteMaintenanceTaskCommand(gymCode, id), cancellationToken);
        return NoContent();
    }
}
