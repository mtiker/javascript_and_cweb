using App.BLL.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.DTO.v1.System.Billing;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SystemAdmin,SystemBilling")]
[Route("api/v{version:apiVersion}/system/subscriptions")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class SubscriptionsController(IPlatformService platformService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SubscriptionSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SubscriptionSummaryResponse>>> GetSubscriptions(CancellationToken cancellationToken)
    {
        return Ok(await platformService.GetSubscriptionsAsync(cancellationToken));
    }

    [HttpPut("{gymId:guid}")]
    [ProducesResponseType(typeof(SubscriptionSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionSummaryResponse>> UpdateSubscription(Guid gymId, [FromBody] UpdateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await platformService.UpdateSubscriptionAsync(gymId, request, cancellationToken));
    }
}
