using App.BLL.Services;
using App.DTO.v1.MemberWorkspace;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MemberWorkspaceController(IMemberWorkspaceService memberWorkspaceService) : ApiControllerBase
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
