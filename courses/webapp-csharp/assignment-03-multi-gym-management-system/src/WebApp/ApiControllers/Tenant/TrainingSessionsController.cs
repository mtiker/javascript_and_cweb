using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.Training.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.TrainingSessions;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class TrainingSessionsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("training-sessions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TrainingSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TrainingSessionResponse>>> GetSessions(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListTrainingSessionsQuery(gymCode), cancellationToken));
    }

    [HttpGet("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> GetSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GetTrainingSessionQuery(gymCode, id), cancellationToken));
    }

    [HttpPost("training-sessions")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TrainingSessionResponse>> CreateSession(string gymCode, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.SendAsync(new CreateTrainingSessionCommand(gymCode, request), cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { version = "1.0", gymCode, id = created.Id }, created);
    }

    [HttpPut("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSession(string gymCode, Guid id, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateTrainingSessionCommand(gymCode, id, request), cancellationToken));
    }

    [HttpDelete("training-sessions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteTrainingSessionCommand(gymCode, id), cancellationToken);
        return NoContent();
    }
}
