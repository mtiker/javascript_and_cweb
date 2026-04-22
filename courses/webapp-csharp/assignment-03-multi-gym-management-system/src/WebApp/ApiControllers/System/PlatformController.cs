using App.BLL.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.DTO.v1.System.Platform;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemBilling,SystemSupport")]
[Route("api/v{version:apiVersion}/system/platform")]
public class PlatformController(IPlatformService platformService) : ControllerBase
{
    [HttpGet("analytics")]
    public async Task<ActionResult<PlatformAnalyticsResponse>> GetAnalytics()
    {
        return Ok(await platformService.GetAnalyticsAsync());
    }
}
