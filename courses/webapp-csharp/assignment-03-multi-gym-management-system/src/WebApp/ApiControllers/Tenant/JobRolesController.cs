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
    public async Task<ActionResult<IReadOnlyCollection<JobRoleResponse>>> GetJobRoles(string gymCode)
    {
        return Ok(await staffWorkflowService.GetJobRolesAsync(gymCode));
    }

    [HttpPost("job-roles")]
    public async Task<ActionResult<JobRoleResponse>> CreateJobRole(string gymCode, [FromBody] JobRoleUpsertRequest request)
    {
        return Ok(await staffWorkflowService.CreateJobRoleAsync(gymCode, request));
    }

    [HttpPut("job-roles/{id:guid}")]
    public async Task<ActionResult<JobRoleResponse>> UpdateJobRole(string gymCode, Guid id, [FromBody] JobRoleUpsertRequest request)
    {
        return Ok(await staffWorkflowService.UpdateJobRoleAsync(gymCode, id, request));
    }

    [HttpDelete("job-roles/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteJobRole(string gymCode, Guid id)
    {
        await staffWorkflowService.DeleteJobRoleAsync(gymCode, id);
        return Ok(new Message("Job role deleted."));
    }
}
