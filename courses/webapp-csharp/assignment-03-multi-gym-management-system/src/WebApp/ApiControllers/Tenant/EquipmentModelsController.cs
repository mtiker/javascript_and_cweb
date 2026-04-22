using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.EquipmentModels;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class EquipmentModelsController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
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
}
