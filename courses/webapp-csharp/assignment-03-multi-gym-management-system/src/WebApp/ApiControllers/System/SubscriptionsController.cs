using App.BLL.Contracts;
using App.DTO.v1.System;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemBilling")]
[Route("api/v{version:apiVersion}/system/subscriptions")]
public class SubscriptionsController(IPlatformService platformService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SubscriptionSummaryResponse>>> GetSubscriptions()
    {
        return Ok(await platformService.GetSubscriptionsAsync());
    }

    [HttpPut("{gymId:guid}")]
    public async Task<ActionResult<SubscriptionSummaryResponse>> UpdateSubscription(Guid gymId, [FromBody] UpdateSubscriptionRequest request)
    {
        return Ok(await platformService.UpdateSubscriptionAsync(gymId, request));
    }
}
