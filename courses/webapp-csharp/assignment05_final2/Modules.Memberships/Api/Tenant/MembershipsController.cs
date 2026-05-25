using App.BLL.Contracts.Services;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.Memberships;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Memberships.Api.Tenant;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/Tenant/MembershipsController.cs</c>
/// in Phase 6. Delegates to the module-owned
/// <see cref="IMembershipWorkflowService"/>. Routes are preserved verbatim.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/{gymCode}")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class MembershipsController(IMembershipWorkflowService membershipWorkflowService) : ControllerBase
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
