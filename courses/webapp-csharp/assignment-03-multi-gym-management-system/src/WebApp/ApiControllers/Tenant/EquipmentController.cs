using App.DTO.v1;
using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.GymManagement.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.Equipment;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class EquipmentController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("equipment")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EquipmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EquipmentResponse>>> GetEquipment(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListEquipmentQuery(gymCode), cancellationToken));
    }

    [HttpPost("equipment")]
    [ProducesResponseType(typeof(EquipmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment(string gymCode, [FromBody] EquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new CreateEquipmentCommand(gymCode, request), cancellationToken));
    }

    [HttpPut("equipment/{id:guid}")]
    [ProducesResponseType(typeof(EquipmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentResponse>> UpdateEquipment(string gymCode, Guid id, [FromBody] EquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateEquipmentCommand(gymCode, id, request), cancellationToken));
    }

    [HttpDelete("equipment/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteEquipment(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteEquipmentCommand(gymCode, id), cancellationToken);
        return Ok(new Message("Equipment deleted."));
    }
}
