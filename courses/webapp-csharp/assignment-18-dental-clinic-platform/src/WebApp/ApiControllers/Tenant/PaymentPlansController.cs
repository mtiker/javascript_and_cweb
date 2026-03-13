using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.PaymentPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class PaymentPlansController(
    AppDbContext dbContext,
    ITenantProvider tenantProvider,
    ISubscriptionPolicyService subscriptionPolicyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PaymentPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentPlanResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);

        var plans = await dbContext.PaymentPlans
            .AsNoTracking()
            .OrderByDescending(entity => entity.StartsAtUtc)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(plans);
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
        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);

        var plan = await dbContext.PaymentPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == paymentPlanId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Payment plan not found."));
        }

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
        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == request.InvoiceId, cancellationToken);
        if (invoice == null)
        {
            return BadRequest(new Message("Invoice does not exist in tenant."));
        }

        var existing = await dbContext.PaymentPlans
            .AsNoTracking()
            .AnyAsync(entity => entity.InvoiceId == request.InvoiceId, cancellationToken);
        if (existing)
        {
            return BadRequest(new Message("Payment plan already exists for this invoice."));
        }

        var plan = new PaymentPlan
        {
            InvoiceId = request.InvoiceId,
            InstallmentCount = request.InstallmentCount,
            InstallmentAmount = request.InstallmentAmount,
            StartsAtUtc = request.StartsAtUtc,
            Status = PaymentPlanStatus.Active,
            Terms = request.Terms.Trim()
        };

        dbContext.PaymentPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

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
        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);
        if (!Enum.TryParse<PaymentPlanStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid payment plan status value."));
        }

        var plan = await dbContext.PaymentPlans
            .SingleOrDefaultAsync(entity => entity.Id == paymentPlanId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Payment plan not found."));
        }

        plan.InstallmentCount = request.InstallmentCount;
        plan.InstallmentAmount = request.InstallmentAmount;
        plan.StartsAtUtc = request.StartsAtUtc;
        plan.Status = status;
        plan.Terms = request.Terms.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(plan));
    }

    [HttpDelete("{paymentPlanId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid paymentPlanId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        await subscriptionPolicyService.EnsureTierAtLeastAsync("PaymentPlans", SubscriptionTier.Standard, cancellationToken);

        var plan = await dbContext.PaymentPlans
            .SingleOrDefaultAsync(entity => entity.Id == paymentPlanId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Payment plan not found."));
        }

        dbContext.PaymentPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static PaymentPlanResponse ToResponse(PaymentPlan entity)
    {
        return new PaymentPlanResponse
        {
            Id = entity.Id,
            InvoiceId = entity.InvoiceId,
            InstallmentCount = entity.InstallmentCount,
            InstallmentAmount = entity.InstallmentAmount,
            StartsAtUtc = entity.StartsAtUtc,
            Status = entity.Status.ToString(),
            Terms = entity.Terms
        };
    }
}
