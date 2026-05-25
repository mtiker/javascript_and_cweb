using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.ModuleApis;

namespace Modules.Gyms.Application;

/// <summary>
/// Phase 5 implementation of <see cref="IGymsModuleApi"/>. Resolves tenant
/// access from the shared <see cref="AppDbContext"/> (Gym + AppUserGymRole).
/// Returns <see cref="GymAccess"/> projections only — never the underlying
/// <c>Gym</c> or <c>AppUserGymRole</c> entities. The shared context dependency
/// is transitional (Phase 5-9); Phase 9 splits per-module persistence and
/// Phase 10 drops the legacy <c>App.*</c> references.
/// </summary>
internal sealed class GymsModuleApiService(AppDbContext dbContext) : IGymsModuleApi
{
    public async Task<GymAccess?> ResolveAccessAsync(
        Guid userId,
        string gymCode,
        IReadOnlyCollection<string>? allowedRoles = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gymCode);

        var gym = await dbContext.Gyms
            .AsNoTracking()
            .Where(entity => entity.Code == gymCode)
            .Select(entity => new { entity.Id, entity.Code, entity.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (gym is null || !gym.IsActive)
        {
            return null;
        }

        var roles = await dbContext.AppUserGymRoles
            .AsNoTracking()
            .Where(role => role.GymId == gym.Id && role.AppUserId == userId && role.IsActive)
            .Select(role => role.RoleName)
            .ToArrayAsync(cancellationToken);

        if (roles.Length == 0)
        {
            return null;
        }

        if (allowedRoles is { Count: > 0 } &&
            !roles.Any(role => allowedRoles.Contains(role, StringComparer.Ordinal)))
        {
            return null;
        }

        return new GymAccess(gym.Id, gym.Code, gym.IsActive, roles);
    }

    public async Task<IReadOnlyList<GymAccess>> ListGymsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.AppUserGymRoles
            .AsNoTracking()
            .Where(role => role.AppUserId == userId && role.IsActive && role.Gym != null && role.Gym.IsActive)
            .Select(role => new
            {
                role.GymId,
                GymCode = role.Gym!.Code,
                GymIsActive = role.Gym.IsActive,
                role.RoleName,
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .GroupBy(row => row.GymId)
            .Select(group =>
            {
                var first = group.First();
                return new GymAccess(
                    first.GymId,
                    first.GymCode,
                    first.GymIsActive,
                    group.Select(row => row.RoleName).Distinct(StringComparer.Ordinal).ToArray());
            })
            .ToArray();
    }

    public Task<GymSettingsSummary?> GetSettingsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.GymSettings
            .AsNoTracking()
            .Where(settings => settings.GymId == gymId)
            .Select(settings => new GymSettingsSummary(
                settings.GymId,
                settings.CurrencyCode,
                settings.AllowNonMemberBookings))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
