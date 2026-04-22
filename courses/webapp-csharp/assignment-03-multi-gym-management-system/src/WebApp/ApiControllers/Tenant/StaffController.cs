using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.Staff;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class StaffController(IStaffWorkflowService staffWorkflowService) : ApiControllerBase
{
    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyCollection<StaffResponse>>> GetStaff(string gymCode)
    {
        return Ok(await staffWorkflowService.GetStaffAsync(gymCode));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<StaffResponse>> CreateStaff(string gymCode, [FromBody] StaffUpsertRequest request)
    {
        return Ok(await staffWorkflowService.CreateStaffAsync(gymCode, request));
    }

    [HttpPut("staff/{id:guid}")]
    public async Task<ActionResult<StaffResponse>> UpdateStaff(string gymCode, Guid id, [FromBody] StaffUpsertRequest request)
    {
        return Ok(await staffWorkflowService.UpdateStaffAsync(gymCode, id, request));
    }

    [HttpDelete("staff/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteStaff(string gymCode, Guid id)
    {
        await staffWorkflowService.DeleteStaffAsync(gymCode, id);
        return Ok(new Message("Staff member deleted."));
    }
}
