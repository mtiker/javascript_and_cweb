using App.BLL.Contracts.Services;
using App.Domain.Enums;
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
    public async Task<ActionResult<IReadOnlyCollection<MembershipResponse>>> GetMemberships(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] MembershipStatus? status = null,
        [FromQuery] Guid? memberId = null,
        [FromQuery] Guid? membershipPackageId = null,
        [FromQuery] DateOnly? startFrom = null,
        [FromQuery] DateOnly? startTo = null)
    {
        var filter = new MembershipFilter
        {
            Status = status,
            MemberId = memberId,
            MembershipPackageId = membershipPackageId,
            StartFrom = startFrom,
            StartTo = startTo
        };
        return Ok(await membershipWorkflowService.GetMembershipsAsync(gymCode, filter, cancellationToken));
    }

    [HttpPost("memberships")]
    [ProducesResponseType(typeof(MembershipSaleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipSaleResponse>> SellMembership(string gymCode, [FromBody] SellMembershipRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.SellMembershipAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("memberships/{id:guid}")]
    [ProducesResponseType(typeof(MembershipResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipResponse>> UpdateMembership(string gymCode, Guid id, [FromBody] MembershipEditRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.UpdateMembershipAsync(gymCode, id, request, cancellationToken));
    }

    [HttpPut("memberships/{id:guid}/status")]
    [ProducesResponseType(typeof(MembershipResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipResponse>> UpdateMembershipStatus(string gymCode, Guid id, [FromBody] MembershipStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.UpdateMembershipStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("memberships/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteMembership(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await membershipWorkflowService.DeleteMembershipAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Membership deleted."));
    }
}
