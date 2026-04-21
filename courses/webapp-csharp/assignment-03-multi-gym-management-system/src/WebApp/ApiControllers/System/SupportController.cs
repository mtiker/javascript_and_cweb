using App.BLL.Contracts;
using App.DTO.v1.System;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/support")]
public class SupportController(IPlatformService platformService) : ControllerBase
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemSupport")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SupportTicketResponse>>> GetTickets()
    {
        return Ok(await platformService.GetSupportTicketsAsync());
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("{gymId:guid}/tickets")]
    public async Task<ActionResult<SupportTicketResponse>> CreateTicket(Guid gymId, [FromBody] SupportTicketRequest request)
    {
        return Ok(await platformService.CreateSupportTicketAsync(gymId, request));
    }
}
