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
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class PlatformController(IPlatformService platformService) : ControllerBase
{
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(PlatformAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformAnalyticsResponse>> GetAnalytics(CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetAnalyticsAsync(cancellationToken));
    }
}
