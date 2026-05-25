using App.BLL.Contracts.Services;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Members;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Memberships.Api.Tenant;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/Tenant/MembersController.cs</c> in
/// Phase 6. Delegates to the module-owned <see cref="IMemberWorkflowService"/>.
/// Routes are preserved verbatim to keep the public API contract stable
/// (asserted by <c>ApiContractMetadataTests</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/{gymCode}/members")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class MembersController(IMemberWorkflowService memberWorkflowService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<MemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MemberResponse>>> GetMembers(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] MemberStatus? status = null)
    {
        var filter = new MemberFilter { Search = search, Status = status };
        return Ok(await memberWorkflowService.GetMembersAsync(gymCode, filter, cancellationToken));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> UpdateMemberStatus(string gymCode, Guid id, [FromBody] MemberStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.UpdateMemberStatusAsync(gymCode, id, request, cancellationToken));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> GetCurrentMember(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.GetCurrentMemberAsync(gymCode, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> GetMember(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.GetMemberAsync(gymCode, id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MemberDetailResponse>> CreateMember(string gymCode, [FromBody] MemberUpsertRequest request, CancellationToken cancellationToken)
    {
        var member = await memberWorkflowService.CreateMemberAsync(gymCode, request, cancellationToken);
        return CreatedAtAction(nameof(GetMember), new { version = "1", gymCode, id = member.Id }, member);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> UpdateMember(string gymCode, Guid id, [FromBody] MemberUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.UpdateMemberAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMember(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await memberWorkflowService.DeleteMemberAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
