using App.BLL.Contracts.Services;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Payments;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Memberships.Api.Tenant;

/// <summary>
/// Relocated from <c>WebApp/ApiControllers/Tenant/PaymentsController.cs</c> in
/// Phase 6. Delegates to the module-owned
/// <see cref="IMembershipWorkflowService"/>; payments are billed as part of
/// the membership workflow. Routes are preserved verbatim.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/{gymCode}")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class PaymentsController(IMembershipWorkflowService membershipWorkflowService) : ControllerBase
{
    [HttpGet("payments")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentResponse>>> GetPayments(
        string gymCode,
        CancellationToken cancellationToken,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] Guid? membershipId = null,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var filter = new PaymentFilter
        {
            Status = status,
            MembershipId = membershipId,
            BookingId = bookingId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        return Ok(await membershipWorkflowService.GetPaymentsAsync(gymCode, filter, cancellationToken));
    }

    [HttpPost("payments")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(string gymCode, [FromBody] PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.CreatePaymentAsync(gymCode, request, cancellationToken));
    }

    [HttpPost("payments/{id:guid}/refund")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> RefundPayment(string gymCode, Guid id, [FromBody] PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        return Ok(await membershipWorkflowService.RefundPaymentAsync(gymCode, id, request, cancellationToken));
    }
}
