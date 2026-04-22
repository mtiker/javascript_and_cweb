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
    public async Task<ActionResult<IReadOnlyCollection<MaintenanceTaskResponse>>> GetMaintenanceTasks(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetMaintenanceTasksAsync(gymCode));
    }

    [HttpPost("maintenance-tasks")]
    public async Task<ActionResult<MaintenanceTaskResponse>> CreateMaintenanceTask(string gymCode, [FromBody] MaintenanceTaskUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateTaskAsync(gymCode, request));
    }

    [HttpPut("maintenance-tasks/{id:guid}/status")]
    public async Task<ActionResult<MaintenanceTaskResponse>> UpdateMaintenanceTaskStatus(string gymCode, Guid id, [FromBody] MaintenanceStatusUpdateRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateTaskStatusAsync(gymCode, id, request));
    }

    [HttpPost("maintenance-tasks/generate-due")]
    public async Task<ActionResult<Message>> GenerateDueTasks(string gymCode)
    {
        var created = await maintenanceWorkflowService.GenerateDueScheduledTasksAsync(gymCode);
        return Ok(new Message($"Created {created} scheduled maintenance tasks."));
    }

    [HttpDelete("maintenance-tasks/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteMaintenanceTask(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteMaintenanceTaskAsync(gymCode, id);
        return Ok(new Message("Maintenance task deleted."));
}
}
