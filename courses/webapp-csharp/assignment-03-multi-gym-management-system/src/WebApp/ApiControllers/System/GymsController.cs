using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System.Support;
using App.DTO.v1.System;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemSupport,SystemBilling")]
[Route("api/v{version:apiVersion}/system/gyms")]
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

    [Authorize(Roles = "SystemAdmin,SystemSupport", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("{gymId:guid}/snapshot")]
    [ProducesResponseType(typeof(CompanySnapshotResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySnapshotResponse>> GetGymSnapshot(Guid gymId, CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetGymSnapshotAsync(gymId, cancellationToken));
    }
}
