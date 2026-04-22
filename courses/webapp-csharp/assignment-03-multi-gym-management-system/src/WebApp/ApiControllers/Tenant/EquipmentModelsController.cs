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
    [ProducesResponseType(typeof(IReadOnlyCollection<EquipmentModelResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EquipmentModelResponse>>> GetEquipmentModels(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.GetEquipmentModelsAsync(gymCode, cancellationToken));
    }

    [HttpPost("equipment-models")]
    [ProducesResponseType(typeof(EquipmentModelResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentModelResponse>> CreateEquipmentModel(string gymCode, [FromBody] EquipmentModelUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.CreateEquipmentModelAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("equipment-models/{id:guid}")]
    [ProducesResponseType(typeof(EquipmentModelResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentModelResponse>> UpdateEquipmentModel(string gymCode, Guid id, [FromBody] EquipmentModelUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await maintenanceWorkflowService.UpdateEquipmentModelAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("equipment-models/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteEquipmentModel(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await maintenanceWorkflowService.DeleteEquipmentModelAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Equipment model deleted."));
}
}
