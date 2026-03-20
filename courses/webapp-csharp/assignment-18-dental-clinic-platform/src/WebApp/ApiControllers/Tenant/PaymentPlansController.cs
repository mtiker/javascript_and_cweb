using App.BLL.Contracts.Finance;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.PaymentPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class PaymentPlansController(
    IPaymentPlanService paymentPlanService,
    ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PaymentPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentPlanResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plans = await paymentPlanService.ListAsync(User.UserId(), invoiceId, cancellationToken);
        return Ok(plans.Select(ToResponse).ToList());
    }

    [HttpGet("{paymentPlanId:guid}")]
    [ProducesResponseType(typeof(PaymentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentPlanResponse>> GetById(
        [FromRoute] string companySlug,
        [FromRoute] Guid paymentPlanId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await paymentPlanService.GetAsync(User.UserId(), paymentPlanId, cancellationToken);
        return Ok(ToResponse(plan));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentPlanResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreatePaymentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await paymentPlanService.CreateAsync(
            User.UserId(),
            new CreatePaymentPlanCommand(
                request.InvoiceId,
                request.StartsAtUtc,
                request.Terms,
                request.Installments.Select(ToCommand).ToArray()),
            cancellationToken);

        return Created(string.Empty, ToResponse(plan));
    }

    [HttpPut("{paymentPlanId:guid}")]
    [ProducesResponseType(typeof(PaymentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentPlanResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid paymentPlanId,
        [FromBody] UpdatePaymentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await paymentPlanService.UpdateAsync(
            User.UserId(),
            new UpdatePaymentPlanCommand(
                paymentPlanId,
                request.StartsAtUtc,
                request.Terms,
                request.Installments.Select(ToCommand).ToArray()),
            cancellationToken);

        return Ok(ToResponse(plan));
    }

    [HttpDelete("{paymentPlanId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid paymentPlanId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        await paymentPlanService.DeleteAsync(User.UserId(), paymentPlanId, cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static PaymentPlanInstallmentCommand ToCommand(PaymentPlanInstallmentRequest request)
    {
        return new PaymentPlanInstallmentCommand(request.DueDateUtc, request.Amount);
    }

    private static PaymentPlanResponse ToResponse(PaymentPlanResult entity)
    {
        return new PaymentPlanResponse
        {
            Id = entity.Id,
            InvoiceId = entity.InvoiceId,
            StartsAtUtc = entity.StartsAtUtc,
            Status = entity.Status,
            Terms = entity.Terms,
            ScheduledAmount = entity.ScheduledAmount,
            RemainingAmount = entity.RemainingAmount,
            Installments = entity.Installments
                .Select(installment => new PaymentPlanInstallmentResponse
                {
                    Id = installment.Id,
                    DueDateUtc = installment.DueDateUtc,
                    Amount = installment.Amount,
                    Status = installment.Status,
                    PaidAtUtc = installment.PaidAtUtc
                })
                .ToArray()
        };
    }
}
