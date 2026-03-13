using App.BLL.Exceptions;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class SubscriptionPolicyService(AppDbContext dbContext, ITenantProvider tenantProvider) : ISubscriptionPolicyService
{
    public async Task EnsureCanCreatePatientAsync(CancellationToken cancellationToken)
    {
        var policy = await ResolveEffectivePolicyAsync(cancellationToken);
        if (!HasEntityLimit(policy))
        {
            return;
        }

        var currentPatients = await dbContext.Patients
            .AsNoTracking()
            .CountAsync(cancellationToken);

        if (currentPatients >= policy.EntityLimit)
        {
            throw new ValidationAppException(
                $"Subscription limit reached: tier '{policy.Tier}' allows up to {policy.EntityLimit} active patients.");
        }
    }

    public async Task EnsureCanAddActiveMembershipAsync(CancellationToken cancellationToken)
    {
        var policy = await ResolveEffectivePolicyAsync(cancellationToken);
        if (!HasUserLimit(policy))
        {
            return;
        }

        var activeUserCount = await dbContext.AppUserRoles
            .AsNoTracking()
            .Where(entity => entity.IsActive)
            .Select(entity => entity.AppUserId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (activeUserCount >= policy.UserLimit)
        {
            throw new ValidationAppException(
                $"Subscription limit reached: tier '{policy.Tier}' allows up to {policy.UserLimit} active users.");
        }
    }

    public async Task EnsureTierAtLeastAsync(string featureName, SubscriptionTier minimumTier, CancellationToken cancellationToken)
    {
        var policy = await ResolveEffectivePolicyAsync(cancellationToken);
        if (policy.Tier >= minimumTier)
        {
            return;
        }

        throw new ValidationAppException(
            $"Feature '{featureName}' requires at least '{minimumTier}' tier. Current tier: '{policy.Tier}'.");
    }

    private async Task<SubscriptionPolicyState> ResolveEffectivePolicyAsync(CancellationToken cancellationToken)
    {
        if (!tenantProvider.CompanyId.HasValue)
        {
            var defaults = ResolveDefaultLimits(SubscriptionTier.Free);
            return new SubscriptionPolicyState(SubscriptionTier.Free, defaults.userLimit, defaults.entityLimit);
        }

        var companyId = tenantProvider.CompanyId.Value;
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId && entity.Status == SubscriptionStatus.Active)
            .OrderByDescending(entity => entity.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            var defaults = ResolveDefaultLimits(SubscriptionTier.Free);
            return new SubscriptionPolicyState(SubscriptionTier.Free, defaults.userLimit, defaults.entityLimit);
        }

        var resolvedLimits = ResolveDefaultLimits(subscription.Tier);
        var userLimit = subscription.Tier == SubscriptionTier.Premium
            ? 0
            : (subscription.UserLimit > 0 ? subscription.UserLimit : resolvedLimits.userLimit);
        var entityLimit = subscription.Tier == SubscriptionTier.Premium
            ? 0
            : (subscription.EntityLimit > 0 ? subscription.EntityLimit : resolvedLimits.entityLimit);

        return new SubscriptionPolicyState(subscription.Tier, userLimit, entityLimit);
    }

    private static bool HasEntityLimit(SubscriptionPolicyState policy)
    {
        if (policy.Tier == SubscriptionTier.Premium)
        {
            return false;
        }

        return policy.EntityLimit > 0;
    }

    private static bool HasUserLimit(SubscriptionPolicyState policy)
    {
        if (policy.Tier == SubscriptionTier.Premium)
        {
            return false;
        }

        return policy.UserLimit > 0;
    }

    private static (int userLimit, int entityLimit) ResolveDefaultLimits(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => (5, 100),
            SubscriptionTier.Standard => (25, 5000),
            SubscriptionTier.Premium => (0, 0),
            _ => (5, 100)
        };
    }

    private sealed record SubscriptionPolicyState(SubscriptionTier Tier, int UserLimit, int EntityLimit);
}
