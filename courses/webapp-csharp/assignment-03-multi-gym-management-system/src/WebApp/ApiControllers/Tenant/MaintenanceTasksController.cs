using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.MaintenanceTasks;

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
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> CreateMaintenanceTask(string gymCode, [FromBody] MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.CreateTaskAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("maintenance-tasks/{id:guid}/status")]
    [ProducesResponseType(typeof(MaintenanceTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskStatus(string gymCode, Guid id, [FromBody] MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateTaskStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPost("maintenance-tasks/generate-due")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> GenerateDueTasks(string gymCode, CancellationToken cancellationToken)
    {
        var created = await maintenanceWorkflowService.GenerateDueScheduledTasksAsync(gymCode, cancellationToken);
        return Ok(new Message($"Created {created} scheduled maintenance tasks."));
    }

    [HttpDelete("maintenance-tasks/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteMaintenanceTask(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteMaintenanceTaskAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Maintenance task deleted."));
}
}
