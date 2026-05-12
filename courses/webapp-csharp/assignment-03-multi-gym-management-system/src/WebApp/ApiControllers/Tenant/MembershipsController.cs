using App.DTO.v1;
using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.MembershipFinance.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.Memberships;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MembershipsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("memberships")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MembershipResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MembershipResponse>>> GetMemberships(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListMembershipsQuery(gymCode), cancellationToken));
    }

    [HttpPost("memberships")]
    [ProducesResponseType(typeof(MembershipSaleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipSaleResponse>> SellMembership(string gymCode, [FromBody] SellMembershipRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new SellMembershipCommand(gymCode, request), cancellationToken));
    }

    [HttpPut("memberships/{id:guid}/status")]
    [ProducesResponseType(typeof(MembershipResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipResponse>> UpdateMembershipStatus(string gymCode, Guid id, [FromBody] MembershipStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateMembershipStatusCommand(gymCode, id, request), cancellationToken));
    }

    [HttpDelete("memberships/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteMembership(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteMembershipCommand(gymCode, id), cancellationToken);
        return Ok(new Message("Membership deleted."));
    }
}
