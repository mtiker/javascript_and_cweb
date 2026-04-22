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
    [ProducesResponseType(typeof(IReadOnlyCollection<MembershipPackageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MembershipPackageResponse>>> GetPackages(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.GetPackagesAsync(gymCode, cancellationToken));
    }

    [HttpPost("membership-packages")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipPackageResponse>> CreatePackage(string gymCode, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.CreatePackageAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("membership-packages/{id:guid}")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipPackageResponse>> UpdatePackage(string gymCode, Guid id, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.UpdatePackageAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("membership-packages/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeletePackage(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await membershipWorkflowService.DeletePackageAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Membership package deleted."));
}
}
