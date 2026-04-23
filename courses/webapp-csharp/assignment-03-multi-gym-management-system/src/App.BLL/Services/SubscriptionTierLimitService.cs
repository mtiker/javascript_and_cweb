using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class SubscriptionTierLimitService(IAppDbContext dbContext) : ISubscriptionTierLimitService
{
    private static readonly IReadOnlyDictionary<SubscriptionPlan, PlanLimits> Limits =
        new Dictionary<SubscriptionPlan, PlanLimits>
        {
            [SubscriptionPlan.Starter] = new(60, 15, 80, 120),
            [SubscriptionPlan.Growth] = new(400, 75, 400, 600),
            [SubscriptionPlan.Enterprise] = new(null, null, null, null)
        };

    public async Task<SubscriptionPlan> GetCurrentPlanAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(entity => entity.GymId == gymId)
            .OrderByDescending(entity => entity.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return subscription?.Plan ?? SubscriptionPlan.Starter;
    }

    public Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(gymId, "members", set => set.Members, dbContext.Members, cancellationToken);

    public Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(gymId, "staff", set => set.Staff, dbContext.Staff, cancellationToken);

    public Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(gymId, "training sessions", set => set.Sessions, dbContext.TrainingSessions, cancellationToken);

    public Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(gymId, "equipment items", set => set.Equipment, dbContext.Equipment, cancellationToken);

    private async Task EnsureWithinLimitAsync<TEntity>(
        Guid gymId,
        string resourceName,
        Func<PlanLimits, int?> limitSelector,
        DbSet<TEntity> entities,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var plan = await GetCurrentPlanAsync(gymId, cancellationToken);
        var limits = Limits[plan];
        var limit = limitSelector(limits);

        if (!limit.HasValue)
        {
            return;
        }

        var count = await entities
            .IgnoreQueryFilters()
            .CountAsync(entity => EF.Property<Guid>(entity, nameof(TenantBaseEntity.GymId)) == gymId &&
                                  !EF.Property<bool>(entity, nameof(TenantBaseEntity.IsDeleted)), cancellationToken);

        if (count >= limit.Value)
        {
            throw new ValidationAppException($"The {plan} subscription allows up to {limit.Value} {resourceName}. Upgrade the plan to add more.");
        }
    }

    private sealed record PlanLimits(int? Members, int? Staff, int? Sessions, int? Equipment);
}
