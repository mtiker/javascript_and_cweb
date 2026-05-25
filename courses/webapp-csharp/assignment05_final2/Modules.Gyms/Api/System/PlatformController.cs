using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.System.Platform;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Gyms.Api.System;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/System/PlatformController.cs</c> in
/// Phase 5. Delegates to the Gyms-owned <see cref="IPlatformService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin")]
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
