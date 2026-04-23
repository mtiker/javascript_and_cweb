using App.BLL.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin")]
[Route("api/v{version:apiVersion}/system/impersonation")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class ImpersonationController(IPlatformService platformService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StartImpersonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StartImpersonationResponse>> Start([FromBody] StartImpersonationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await platformService.StartImpersonationAsync(request, cancellationToken));
    }
}
