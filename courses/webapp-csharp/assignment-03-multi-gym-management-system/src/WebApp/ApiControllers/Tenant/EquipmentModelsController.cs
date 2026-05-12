using App.DTO.v1;
using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.GymManagement.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.EquipmentModels;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class EquipmentModelsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("equipment-models")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EquipmentModelResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EquipmentModelResponse>>> GetEquipmentModels(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListEquipmentModelsQuery(gymCode), cancellationToken));
    }

    [HttpPost("equipment-models")]
    [ProducesResponseType(typeof(EquipmentModelResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentModelResponse>> CreateEquipmentModel(string gymCode, [FromBody] EquipmentModelUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new CreateEquipmentModelCommand(gymCode, request), cancellationToken));
    }

    [HttpPut("equipment-models/{id:guid}")]
    [ProducesResponseType(typeof(EquipmentModelResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentModelResponse>> UpdateEquipmentModel(string gymCode, Guid id, [FromBody] EquipmentModelUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateEquipmentModelCommand(gymCode, id, request), cancellationToken));
    }

    [HttpDelete("equipment-models/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteEquipmentModel(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteEquipmentModelCommand(gymCode, id), cancellationToken);
        return Ok(new Message("Equipment model deleted."));
    }
}
