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
    [ProducesResponseType(typeof(IReadOnlyCollection<MembershipResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MembershipResponse>>> GetMemberships(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.GetMembershipsAsync(gymCode, cancellationToken));
    }

    [HttpPost("memberships")]
    [ProducesResponseType(typeof(MembershipSaleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipSaleResponse>> SellMembership(string gymCode, [FromBody] SellMembershipRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.SellMembershipAsync(gymCode, request, cancellationToken));
    }

    [HttpDelete("memberships/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteMembership(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await membershipWorkflowService.DeleteMembershipAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Membership deleted."));
    }
}
