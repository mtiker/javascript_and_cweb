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
    public async Task<ActionResult<IReadOnlyCollection<TrainingCategoryResponse>>> GetCategories(string gymCode)
    {
        return Ok(await trainingWorkflowService.GetCategoriesAsync(gymCode));
    }

    [HttpPost("training-categories")]
    public async Task<ActionResult<TrainingCategoryResponse>> CreateCategory(string gymCode, [FromBody] TrainingCategoryUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.CreateCategoryAsync(gymCode, request));
    }

    [HttpPut("training-categories/{id:guid}")]
    public async Task<ActionResult<TrainingCategoryResponse>> UpdateCategory(string gymCode, Guid id, [FromBody] TrainingCategoryUpsertRequest request)
    {
        return Ok(await trainingWorkflowService.UpdateCategoryAsync(gymCode, id, request));
    }

    [HttpDelete("training-categories/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteCategory(string gymCode, Guid id)
    {
        await trainingWorkflowService.DeleteCategoryAsync(gymCode, id);
        return Ok(new Message("Training category deleted."));
}
}
