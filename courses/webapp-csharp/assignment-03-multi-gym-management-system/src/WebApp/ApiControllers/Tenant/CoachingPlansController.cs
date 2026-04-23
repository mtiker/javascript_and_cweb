using App.BLL.Services;
using App.DTO.v1.CoachingPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class CoachingPlansController(ICoachingPlanService coachingPlanService) : ApiControllerBase
{
    [HttpGet("coaching-plans")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CoachingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CoachingPlanResponse>>> GetPlans(string gymCode, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
    {
        return Ok(await coachingPlanService.GetPlansAsync(gymCode, memberId, cancellationToken));
    }

    [HttpGet("coaching-plans/{id:guid}")]
    [ProducesResponseType(typeof(CoachingPlanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachingPlanResponse>> GetPlan(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await coachingPlanService.GetPlanAsync(gymCode, id, cancellationToken));
    }

    [HttpPost("coaching-plans")]
    [ProducesResponseType(typeof(CoachingPlanResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CoachingPlanResponse>> CreatePlan(string gymCode, [FromBody] CoachingPlanCreateRequest request, CancellationToken cancellationToken)
    {
        var created = await coachingPlanService.CreatePlanAsync(gymCode, request, cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { version = "1.0", gymCode, id = created.Id }, created);
    }

    [HttpPut("coaching-plans/{id:guid}")]
    [ProducesResponseType(typeof(CoachingPlanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachingPlanResponse>> UpdatePlan(string gymCode, Guid id, [FromBody] CoachingPlanUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await coachingPlanService.UpdatePlanAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("coaching-plans/{id:guid}/status")]
    [ProducesResponseType(typeof(CoachingPlanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachingPlanResponse>> UpdatePlanStatus(string gymCode, Guid id, [FromBody] CoachingPlanStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await coachingPlanService.UpdatePlanStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("coaching-plans/{id:guid}/items/{itemId:guid}/decision")]
    [ProducesResponseType(typeof(CoachingPlanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachingPlanResponse>> DecidePlanItem(string gymCode, Guid id, Guid itemId, [FromBody] CoachingPlanItemDecisionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await coachingPlanService.DecidePlanItemAsync(gymCode, id, itemId, request, cancellationToken));
    }

    [HttpDelete("coaching-plans/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePlan(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await coachingPlanService.DeletePlanAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
