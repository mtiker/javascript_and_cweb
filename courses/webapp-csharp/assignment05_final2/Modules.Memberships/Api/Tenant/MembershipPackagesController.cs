using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Memberships.Api.Tenant;

/// <summary>
/// Relocated from
/// <c>WebApp/ApiControllers/Tenant/MembershipPackagesController.cs</c> in
/// Phase 6. Delegates to the module-owned
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
public class MembershipPackagesController(IMembershipWorkflowService membershipWorkflowService) : ControllerBase
{
    [HttpGet("membership-packages")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MembershipPackageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MembershipPackageResponse>>> GetPackages(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.GetPackagesAsync(gymCode, cancellationToken));
    }

    [HttpPost("membership-packages")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MembershipPackageResponse>> CreatePackage(string gymCode, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await membershipWorkflowService.CreatePackageAsync(gymCode, request, cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPut("membership-packages/{id:guid}")]
    [ProducesResponseType(typeof(MembershipPackageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipPackageResponse>> UpdatePackage(string gymCode, Guid id, [FromBody] MembershipPackageUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.UpdatePackageAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("membership-packages/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePackage(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await membershipWorkflowService.DeletePackageAsync(gymCode, id, cancellationToken);
        return NoContent();
    }
}
