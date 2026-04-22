using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.Memberships;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MembershipsController(IMembershipWorkflowService membershipWorkflowService) : ApiControllerBase
{
    [HttpGet("memberships")]
    public async Task<ActionResult<IReadOnlyCollection<MembershipResponse>>> GetMemberships(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetMembershipsAsync(gymCode));
    }

    [HttpPost("memberships")]
    public async Task<ActionResult<MembershipSaleResponse>> SellMembership(string gymCode, [FromBody] SellMembershipRequest request)
    {
        return Ok(await membershipWorkflowService.SellMembershipAsync(gymCode, request));
    }

    [HttpDelete("memberships/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteMembership(string gymCode, Guid id)
    {
        await membershipWorkflowService.DeleteMembershipAsync(gymCode, id);
        return Ok(new Message("Membership deleted."));
    }
}
