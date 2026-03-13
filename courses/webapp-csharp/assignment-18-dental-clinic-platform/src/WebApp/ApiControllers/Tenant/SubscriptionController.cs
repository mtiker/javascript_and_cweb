using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.Subscriptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
public class SubscriptionController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(TenantSubscriptionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantSubscriptionResponse>> Get([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var subscription = await GetOrCreateSubscriptionAsync(cancellationToken);
        return Ok(ToResponse(subscription));
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner)]
    [ProducesResponseType(typeof(TenantSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantSubscriptionResponse>> UpdateTier(
        [FromRoute] string companySlug,
        [FromBody] UpdateTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (!SubscriptionTierDefaults.TryParseTier(request.Tier, out var tier))
        {
            return BadRequest(new Message("Invalid subscription tier."));
        }

        var subscription = await GetOrCreateSubscriptionAsync(cancellationToken);
        var limits = SubscriptionTierDefaults.ResolveLimits(tier);

        subscription.Tier = tier;
        subscription.Status = SubscriptionStatus.Active;
        subscription.UserLimit = limits.userLimit;
        subscription.EntityLimit = limits.entityLimit;
        subscription.EndsAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(subscription));
    }

    private async Task<Subscription> GetOrCreateSubscriptionAsync(CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .OrderByDescending(entity => entity.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription != null)
        {
            return subscription;
        }

        if (!tenantProvider.CompanyId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var defaults = SubscriptionTierDefaults.ResolveLimits(SubscriptionTier.Free);
        subscription = new Subscription
        {
            CompanyId = tenantProvider.CompanyId.Value,
            Tier = SubscriptionTier.Free,
            Status = SubscriptionStatus.Active,
            UserLimit = defaults.userLimit,
            EntityLimit = defaults.entityLimit
        };

        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static TenantSubscriptionResponse ToResponse(Subscription subscription)
    {
        return new TenantSubscriptionResponse
        {
            Id = subscription.Id,
            CompanyId = subscription.CompanyId,
            Tier = subscription.Tier.ToString(),
            Status = subscription.Status.ToString(),
            StartsAtUtc = subscription.StartsAtUtc,
            EndsAtUtc = subscription.EndsAtUtc,
            UserLimit = subscription.UserLimit,
            EntityLimit = subscription.EntityLimit
        };
    }
}
