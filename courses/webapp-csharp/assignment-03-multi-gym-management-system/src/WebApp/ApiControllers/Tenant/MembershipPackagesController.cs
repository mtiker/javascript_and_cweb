using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.MembershipPackages;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MembershipPackagesController(IMembershipWorkflowService membershipWorkflowService) : ApiControllerBase
{
    [HttpGet("membership-packages")]
    public async Task<ActionResult<IReadOnlyCollection<MembershipPackageResponse>>> GetPackages(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetPackagesAsync(gymCode));
    }

    [HttpPost("membership-packages")]
    public async Task<ActionResult<MembershipPackageResponse>> CreatePackage(string gymCode, [FromBody] MembershipPackageUpsertRequest request)
    {
        return Ok(await membershipWorkflowService.CreatePackageAsync(gymCode, request));
    }

    [HttpPut("membership-packages/{id:guid}")]
    public async Task<ActionResult<MembershipPackageResponse>> UpdatePackage(string gymCode, Guid id, [FromBody] MembershipPackageUpsertRequest request)
    {
        return Ok(await membershipWorkflowService.UpdatePackageAsync(gymCode, id, request));
    }

    [HttpDelete("membership-packages/{id:guid}")]
    public async Task<ActionResult<Message>> DeletePackage(string gymCode, Guid id)
    {
        await membershipWorkflowService.DeletePackageAsync(gymCode, id);
        return Ok(new Message("Membership package deleted."));
}
}
