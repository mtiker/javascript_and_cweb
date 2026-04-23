using App.BLL.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.DTO.v1.System.Support;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/support")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class SupportController(IPlatformService platformService) : ControllerBase
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemSupport")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SupportTicketResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SupportTicketResponse>>> GetTickets(CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetSupportTicketsAsync(cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("{gymId:guid}/tickets")]
    [ProducesResponseType(typeof(SupportTicketResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketResponse>> CreateTicket(Guid gymId, [FromBody] SupportTicketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await platformService.CreateSupportTicketAsync(gymId, request, cancellationToken));
    }
}
