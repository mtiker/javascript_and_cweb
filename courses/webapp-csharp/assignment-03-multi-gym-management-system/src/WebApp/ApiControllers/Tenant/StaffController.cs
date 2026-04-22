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
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StaffResponse>>> GetStaff(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.GetStaffAsync(gymCode, cancellationToken));
    }

    [HttpPost("staff")]
    [ProducesResponseType(typeof(StaffResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffResponse>> CreateStaff(string gymCode, [FromBody] StaffUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.CreateStaffAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("staff/{id:guid}")]
    [ProducesResponseType(typeof(StaffResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffResponse>> UpdateStaff(string gymCode, Guid id, [FromBody] StaffUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.UpdateStaffAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("staff/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteStaff(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await staffWorkflowService.DeleteStaffAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Staff member deleted."));
    }
}
