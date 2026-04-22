using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.Members;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}/members")]
public class MembersController(IMemberWorkflowService memberWorkflowService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<MemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MemberResponse>>> GetMembers(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.GetMembersAsync(gymCode, cancellationToken));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status201Created)]
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
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> CreateMember(string gymCode, [FromBody] MemberUpsertRequest request, CancellationToken cancellationToken)
    {
        var member = await memberWorkflowService.CreateMemberAsync(gymCode, request, cancellationToken);
        return CreatedAtAction(nameof(GetMember), new { version = "1.0", gymCode, id = member.Id }, member);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDetailResponse>> UpdateMember(string gymCode, Guid id, [FromBody] MemberUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await memberWorkflowService.UpdateMemberAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteMember(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await memberWorkflowService.DeleteMemberAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Member deleted."));
    }
}
