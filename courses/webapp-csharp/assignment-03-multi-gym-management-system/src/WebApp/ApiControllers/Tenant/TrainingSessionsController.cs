using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.TrainingSessions;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class TrainingSessionsController(ITrainingWorkflowService trainingWorkflowService) : ApiControllerBase
{
    [HttpGet("training-sessions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TrainingSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TrainingSessionResponse>>> GetSessions(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetSessionsAsync(gymCode, cancellationToken));
    }

    [HttpGet("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> GetSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetSessionAsync(gymCode, id, cancellationToken));
    }

    [HttpPost("training-sessions")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> CreateSession(string gymCode, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, null, request, cancellationToken));
    }

    [HttpPut("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSession(string gymCode, Guid id, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.DeleteSessionAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Training session deleted."));
}
}
