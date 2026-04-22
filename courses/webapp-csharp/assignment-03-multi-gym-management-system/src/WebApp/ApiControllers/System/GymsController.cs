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
    public async Task<ActionResult<IReadOnlyCollection<GymSummaryResponse>>> GetGyms()
    {
        return Ok(await platformService.GetGymsAsync());
    }

    [Authorize(Roles = "SystemAdmin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    public async Task<ActionResult<RegisterGymResponse>> RegisterGym([FromBody] RegisterGymRequest request)
    {
        var response = await platformService.RegisterGymAsync(request);
        return CreatedAtAction(nameof(GetGymSnapshot), new { version = "1.0", gymId = response.GymId }, response);
    }

    [Authorize(Roles = "SystemAdmin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("{gymId:guid}/activation")]
    public async Task<ActionResult<Message>> UpdateActivation(Guid gymId, [FromBody] UpdateGymActivationRequest request)
    {
        await platformService.UpdateGymActivationAsync(gymId, request);
        return Ok(new Message("Gym activation updated."));
    }

    [Authorize(Roles = "SystemAdmin,SystemSupport", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("{gymId:guid}/snapshot")]
    public async Task<ActionResult<CompanySnapshotResponse>> GetGymSnapshot(Guid gymId)
    {
        return Ok(await platformService.GetGymSnapshotAsync(gymId));
    }
}
