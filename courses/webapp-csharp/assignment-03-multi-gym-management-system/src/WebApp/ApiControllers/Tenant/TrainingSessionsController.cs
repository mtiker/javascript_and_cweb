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
    public async Task<ActionResult<IReadOnlyCollection<TrainingSessionResponse>>> GetSessions(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetSessionsAsync(gymCode));
    }

    [HttpGet("training-sessions/{id:guid}")]
    public async Task<ActionResult<TrainingSessionResponse>> GetSession(string gymCode, Guid id)
    {
        return Ok(await trainingWorkflowService.GetSessionAsync(gymCode, id));
    }

    [HttpPost("training-sessions")]
    public async Task<ActionResult<TrainingSessionResponse>> CreateSession(string gymCode, [FromBody] TrainingSessionUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, null, request));
    }

    [HttpPut("training-sessions/{id:guid}")]
    public async Task<ActionResult<TrainingSessionResponse>> UpdateSession(string gymCode, Guid id, [FromBody] TrainingSessionUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, id, request));
    }

    [HttpDelete("training-sessions/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteSession(string gymCode, Guid id)
    {
        await trainingWorkflowService.DeleteSessionAsync(gymCode, id);
        return Ok(new Message("Training session deleted."));
}
}
