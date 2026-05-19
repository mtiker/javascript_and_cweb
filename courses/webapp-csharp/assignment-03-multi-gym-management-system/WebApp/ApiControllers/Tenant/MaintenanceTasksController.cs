using App.BLL.Contracts.Services;
using App.Domain.Enums;
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
    public async Task<ActionResult<IReadOnlyCollection<MaintenanceTaskResponse>>> GetMaintenanceTasks(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] MaintenanceTaskStatus? status = null,
        [FromQuery] MaintenancePriority? priority = null,
        [FromQuery] MaintenanceTaskType? taskType = null,
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] Guid? assignedStaffId = null,
        [FromQuery] DateTime? dueBeforeUtc = null)
    {
        var filter = new MaintenanceTaskFilter
        {
            Status = status,
            Priority = priority,
            TaskType = taskType,
            EquipmentId = equipmentId,
            AssignedStaffId = assignedStaffId,
            DueBeforeUtc = dueBeforeUtc
        };
        return Ok(await maintenanceWorkflowService.GetMaintenanceTasksAsync(gymCode, filter, cancellationToken));
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
