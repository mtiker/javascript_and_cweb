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
    public async Task<ActionResult<IReadOnlyCollection<MemberResponse>>> GetMembers(string gymCode)
    {
        return Ok(await memberWorkflowService.GetMembersAsync(gymCode));
    }

    [HttpGet("me")]
    public async Task<ActionResult<MemberDetailResponse>> GetCurrentMember(string gymCode)
    {
        return Ok(await memberWorkflowService.GetCurrentMemberAsync(gymCode));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberDetailResponse>> GetMember(string gymCode, Guid id)
    {
        return Ok(await memberWorkflowService.GetMemberAsync(gymCode, id));
    }

    [HttpPost]
    public async Task<ActionResult<MemberDetailResponse>> CreateMember(string gymCode, [FromBody] MemberUpsertRequest request)
    {
        var member = await memberWorkflowService.CreateMemberAsync(gymCode, request);
        return CreatedAtAction(nameof(GetMember), new { version = "1.0", gymCode, id = member.Id }, member);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemberDetailResponse>> UpdateMember(string gymCode, Guid id, [FromBody] MemberUpsertRequest request)
    {
        return Ok(await memberWorkflowService.UpdateMemberAsync(gymCode, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Message>> DeleteMember(string gymCode, Guid id)
    {
        await memberWorkflowService.DeleteMemberAsync(gymCode, id);
        return Ok(new Message("Member deleted."));
    }
}
