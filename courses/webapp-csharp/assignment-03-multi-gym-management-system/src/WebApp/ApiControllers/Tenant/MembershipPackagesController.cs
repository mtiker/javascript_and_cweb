using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.MembershipFinance.Contracts;
using WebApp.ApiControllers;
using App.DTO.v1.MembershipPackages;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MembershipPackagesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("membership-packages")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MembershipPackageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MembershipPackageResponse>>> GetPackages(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListMembershipPackagesQuery(gymCode), cancellationToken));
    }

    [HttpPost("membership-packages")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MembershipPackageResponse>> CreatePackage(string gymCode, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.SendAsync(new CreateMembershipPackageCommand(gymCode, request), cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPut("membership-packages/{id:guid}")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipPackageResponse>> UpdatePackage(string gymCode, Guid id, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new UpdateMembershipPackageCommand(gymCode, id, request), cancellationToken));
    }

    [HttpDelete("membership-packages/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePackage(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new DeleteMembershipPackageCommand(gymCode, id), cancellationToken);
        return NoContent();
    }
}
