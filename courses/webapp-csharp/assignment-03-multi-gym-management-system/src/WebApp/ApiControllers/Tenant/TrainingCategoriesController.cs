using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.TrainingCategories;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class TrainingCategoriesController(ITrainingWorkflowService trainingWorkflowService) : ApiControllerBase
{
    [HttpGet("training-categories")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TrainingCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TrainingCategoryResponse>>> GetCategories(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.GetCategoriesAsync(gymCode, cancellationToken));
    }

    [HttpPost("training-categories")]
    [ProducesResponseType(typeof(TrainingCategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingCategoryResponse>> CreateCategory(string gymCode, [FromBody] TrainingCategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.CreateCategoryAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("training-categories/{id:guid}")]
    [ProducesResponseType(typeof(TrainingCategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingCategoryResponse>> UpdateCategory(string gymCode, Guid id, [FromBody] TrainingCategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trainingWorkflowService.UpdateCategoryAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("training-categories/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteCategory(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await trainingWorkflowService.DeleteCategoryAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Training category deleted."));
}
}
