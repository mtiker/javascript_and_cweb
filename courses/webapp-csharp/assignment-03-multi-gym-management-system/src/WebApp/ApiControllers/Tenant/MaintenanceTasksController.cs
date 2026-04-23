using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.MaintenanceTasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MaintenanceTasksController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
    [HttpGet("maintenance-tasks")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MaintenanceTaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MaintenanceTaskResponse>>> GetMaintenanceTasks(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetMaintenanceTasksAsync(gymCode, cancellationToken));
    }

    [HttpPost("maintenance-tasks")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MaintenanceTaskResponse>> CreateMaintenanceTask(string gymCode, [FromBody] MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await maintenanceWorkflowService.CreateTaskAsync(gymCode, request, cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPut("maintenance-tasks/{id:guid}/status")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskStatus(string gymCode, Guid id, [FromBody] MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateTaskStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("maintenance-tasks/{id:guid}/assignment")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskAssignment(string gymCode, Guid id, [FromBody] MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateTaskAssignmentAsync(gymCode, id, request, cancellationToken));
    }

    [HttpGet("maintenance-tasks/{id:guid}/assignment-history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>> GetMaintenanceTaskAssignmentHistory(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetTaskAssignmentHistoryAsync(gymCode, id, cancellationToken));
    }

    [HttpPost("maintenance-tasks/generate-due")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> GenerateDueTasks(string gymCode, CancellationToken cancellationToken)
    {
        var created = await maintenanceWorkflowService.GenerateDueScheduledTasksAsync(gymCode, cancellationToken);
        return Ok(new Message($"Created {created} scheduled maintenance tasks."));
    }

    [HttpDelete("maintenance-tasks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMaintenanceTask(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteMaintenanceTaskAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
