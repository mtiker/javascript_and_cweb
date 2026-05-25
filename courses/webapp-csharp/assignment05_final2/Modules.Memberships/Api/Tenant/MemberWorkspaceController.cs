using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.MemberWorkspace;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Memberships.Api.Tenant;

/// <summary>
/// Relocated from
/// <c>WebApp/ApiControllers/Tenant/MemberWorkspaceController.cs</c> in
/// Phase 6. Delegates to the module-owned
/// <see cref="IMemberWorkspaceService"/>. Routes are preserved verbatim.
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
public class MemberWorkspaceController(IMemberWorkspaceService memberWorkspaceService) : ControllerBase
{
    [HttpGet("member-workspace/me")]
    [ProducesResponseType(typeof(MemberWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberWorkspaceResponse>> GetCurrentWorkspace(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkspaceService.GetCurrentWorkspaceAsync(gymCode, cancellationToken));
    }

    [HttpGet("member-workspace/members/{memberId:guid}")]
    [ProducesResponseType(typeof(MemberWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberWorkspaceResponse>> GetMemberWorkspace(string gymCode, Guid memberId, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkspaceService.GetWorkspaceAsync(gymCode, memberId, cancellationToken));
    }
}
