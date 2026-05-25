using App.BLL.Contracts.Services;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.TrainingSessions;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Training.Api;

namespace Modules.Training.Api.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class TrainingSessionsController(ITrainingWorkflowService trainingWorkflowService) : TrainingApiControllerBase
{
    [HttpGet("training-sessions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TrainingSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TrainingSessionResponse>>> GetSessions(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] TrainingSessionStatus? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? trainerStaffId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var filter = new TrainingSessionFilter
        {
            Status = status,
            CategoryId = categoryId,
            TrainerStaffId = trainerStaffId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        return Ok(await trainingWorkflowService.GetSessionsAsync(gymCode, filter, cancellationToken));
    }

    [HttpGet("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> GetSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetSessionAsync(gymCode, id, cancellationToken));
    }

    [HttpPost("training-sessions")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TrainingSessionResponse>> CreateSession(string gymCode, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, null, request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { version = "1.0", gymCode, id = created.Id }, created);
    }

    [HttpPut("training-sessions/{id:guid}")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSession(string gymCode, Guid id, [FromBody] TrainingSessionUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("training-sessions/{id:guid}/status")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSessionStatus(string gymCode, Guid id, [FromBody] TrainingSessionStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpdateSessionStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("training-sessions/{id:guid}/trainer")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSessionTrainer(string gymCode, Guid id, [FromBody] TrainingSessionTrainerUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpdateSessionTrainerAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("training-sessions/{id:guid}/reschedule")]
    [ProducesResponseType(typeof(TrainingSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingSessionResponse>> RescheduleSession(string gymCode, Guid id, [FromBody] TrainingSessionRescheduleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.RescheduleSessionAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("training-sessions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSession(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.DeleteSessionAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
