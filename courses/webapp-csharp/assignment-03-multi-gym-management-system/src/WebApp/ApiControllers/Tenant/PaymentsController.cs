using App.BLL.Services;
using App.DTO.v1;
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
    public async Task<ActionResult<IReadOnlyCollection<PaymentResponse>>> GetPayments(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetPaymentsAsync(gymCode));
    }

    [HttpPost("payments")]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(string gymCode, [FromBody] PaymentCreateRequest request)
    {
        return Ok(await membershipWorkflowService.CreatePaymentAsync(gymCode, request));
}
}
