using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.System;
using Shared.Contracts.Dtos.v1.System.Platform;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Gyms.Api.System;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/System/GymsController.cs</c> in
/// Phase 5. Delegates to the Gyms-owned <see cref="IPlatformService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin")]
[Route("api/v{version:apiVersion}/system/gyms")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class GymsController(IPlatformService platformService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GymSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GymSummaryResponse>>> GetGyms(CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetGymsAsync(cancellationToken));
    }

    [Authorize(Roles = "SystemAdmin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    [ProducesResponseType(typeof(RegisterGymResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterGymResponse>> RegisterGym([FromBody] RegisterGymRequest request, CancellationToken cancellationToken)
    {
        var response = await platformService.RegisterGymAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetGymSnapshot), new { version = "1.0", gymId = response.GymId }, response);
    }

    [Authorize(Roles = "SystemAdmin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("{gymId:guid}/activation")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> UpdateActivation(Guid gymId, [FromBody] UpdateGymActivationRequest request, CancellationToken cancellationToken)
    {
        await platformService.UpdateGymActivationAsync(gymId, request, cancellationToken);
        return Ok(new Message("Gym activation updated."));
    }

    [Authorize(Roles = "SystemAdmin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("{gymId:guid}/snapshot")]
    [ProducesResponseType(typeof(CompanySnapshotResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySnapshotResponse>> GetGymSnapshot(Guid gymId, CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetGymSnapshotAsync(gymId, cancellationToken));
    }
}
