using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.Equipment;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class EquipmentController(IMaintenanceWorkflowService maintenanceWorkflowService) : ApiControllerBase
{
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
}
