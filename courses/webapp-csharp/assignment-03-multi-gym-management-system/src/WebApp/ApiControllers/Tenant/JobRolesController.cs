using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.JobRoles;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class JobRolesController(IStaffWorkflowService staffWorkflowService) : ApiControllerBase
{
    [HttpGet("job-roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<JobRoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<JobRoleResponse>>> GetJobRoles(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.GetJobRolesAsync(gymCode, cancellationToken));
    }

    [HttpPost("job-roles")]
    [ProducesResponseType(typeof(JobRoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobRoleResponse>> CreateJobRole(string gymCode, [FromBody] JobRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.CreateJobRoleAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("job-roles/{id:guid}")]
    [ProducesResponseType(typeof(JobRoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobRoleResponse>> UpdateJobRole(string gymCode, Guid id, [FromBody] JobRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.UpdateJobRoleAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("job-roles/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteJobRole(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await staffWorkflowService.DeleteJobRoleAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Job role deleted."));
    }
}
