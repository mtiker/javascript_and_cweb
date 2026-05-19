using App.BLL.Contracts.Services;
using App.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;
using App.DTO.v1.Payments;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class PaymentsController(IMembershipWorkflowService membershipWorkflowService) : ApiControllerBase
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
