using App.BLL.Contracts;
using App.DTO.v1;
using App.DTO.v1.Tenant;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class FacilitiesController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
    [HttpGet("opening-hours")]
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursResponse>>> GetOpeningHours(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHoursAsync(gymCode));
    }

    [HttpPost("opening-hours")]
    public async Task<ActionResult<OpeningHoursResponse>> CreateOpeningHours(string gymCode, [FromBody] OpeningHoursUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHoursAsync(gymCode, request));
    }

    [HttpPut("opening-hours/{id:guid}")]
    public async Task<ActionResult<OpeningHoursResponse>> UpdateOpeningHours(string gymCode, Guid id, [FromBody] OpeningHoursUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHoursAsync(gymCode, id, request));
    }

    [HttpDelete("opening-hours/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteOpeningHours(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteOpeningHoursAsync(gymCode, id);
        return Ok(new Message("Opening hours deleted."));
    }

    [HttpGet("opening-hours-exceptions")]
    public async Task<ActionResult<IReadOnlyCollection<OpeningHoursExceptionResponse>>> GetOpeningHourExceptions(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetOpeningHourExceptionsAsync(gymCode));
    }

    [HttpPost("opening-hours-exceptions")]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> CreateOpeningHourException(string gymCode, [FromBody] OpeningHoursExceptionUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateOpeningHourExceptionAsync(gymCode, request));
    }

    [HttpPut("opening-hours-exceptions/{id:guid}")]
    public async Task<ActionResult<OpeningHoursExceptionResponse>> UpdateOpeningHourException(string gymCode, Guid id, [FromBody] OpeningHoursExceptionUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateOpeningHourExceptionAsync(gymCode, id, request));
    }

    [HttpDelete("opening-hours-exceptions/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteOpeningHourException(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteOpeningHourExceptionAsync(gymCode, id);
        return Ok(new Message("Opening hours exception deleted."));
    }

    [HttpGet("equipment-models")]
    public async Task<ActionResult<IReadOnlyCollection<EquipmentModelResponse>>> GetEquipmentModels(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetEquipmentModelsAsync(gymCode));
    }

    [HttpPost("equipment-models")]
    public async Task<ActionResult<EquipmentModelResponse>> CreateEquipmentModel(string gymCode, [FromBody] EquipmentModelUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateEquipmentModelAsync(gymCode, request));
    }

    [HttpPut("equipment-models/{id:guid}")]
    public async Task<ActionResult<EquipmentModelResponse>> UpdateEquipmentModel(string gymCode, Guid id, [FromBody] EquipmentModelUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateEquipmentModelAsync(gymCode, id, request));
    }

    [HttpDelete("equipment-models/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteEquipmentModel(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteEquipmentModelAsync(gymCode, id);
        return Ok(new Message("Equipment model deleted."));
    }

    [HttpGet("equipment")]
    public async Task<ActionResult<IReadOnlyCollection<EquipmentResponse>>> GetEquipment(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetEquipmentAsync(gymCode));
    }

    [HttpPost("equipment")]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment(string gymCode, [FromBody] EquipmentUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.CreateEquipmentAsync(gymCode, request));
    }

    [HttpPut("equipment/{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> UpdateEquipment(string gymCode, Guid id, [FromBody] EquipmentUpsertRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateEquipmentAsync(gymCode, id, request));
    }

    [HttpDelete("equipment/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteEquipment(string gymCode, Guid id)
    {
        await maintenanceWorkflowService.DeleteEquipmentAsync(gymCode, id);
        return Ok(new Message("Equipment deleted."));
    }

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

    [HttpGet("gym-settings")]
    public async Task<ActionResult<GymSettingsResponse>> GetGymSettings(string gymCode)
    {
        return Ok(await maintenanceWorkflowService.GetGymSettingsAsync(gymCode));
    }

    [HttpPut("gym-settings")]
    public async Task<ActionResult<GymSettingsResponse>> UpdateGymSettings(string gymCode, [FromBody] GymSettingsUpdateRequest request)
    {
        return Ok(await maintenanceWorkflowService.UpdateGymSettingsAsync(gymCode, request));
    }

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
