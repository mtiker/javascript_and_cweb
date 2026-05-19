using App.BLL.Contracts.Infrastructure;
using App.Domain;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public sealed class WorkspaceContextService(IAppDbContext dbContext) : IWorkspaceContextService
{
    public Task<AppUserGymRole?> FindDefaultActiveLinkAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == userId && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AppUserGymRole?> FindUserGymLinkAsync(Guid userId, string gymCode, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == userId && link.IsActive && link.Gym!.Code == gymCode)
            .OrderBy(link => link.RoleName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AppUserGymRole?> FindUserGymRoleLinkAsync(Guid userId, string gymCode, string roleName, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .FirstOrDefaultAsync(
                link =>
                    link.AppUserId == userId &&
                    link.IsActive &&
                    link.RoleName == roleName &&
                    link.Gym!.Code == gymCode,
                cancellationToken);
    }

    public async Task<AppUserGymRole?> BuildSystemAdminGymRoleAsync(
        Guid userId,
        string gymCode,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (!IsSystemAdminTenantRole(roleName))
        {
            return null;
        }

        var gym = await dbContext.Gyms
            .FirstOrDefaultAsync(entity => entity.Code == gymCode && entity.IsActive, cancellationToken);
        if (gym == null)
        {
            return null;
        }

        return new AppUserGymRole
        {
            AppUserId = userId,
            GymId = gym.Id,
            Gym = gym,
            RoleName = roleName,
            IsActive = true
        };
    }

    public async Task<WorkspaceSwitchOptions> GetSwitchOptionsAsync(
        Guid userId,
        bool isSystemAdmin,
        string? activeGymCode,
        CancellationToken cancellationToken = default)
    {
        var links = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == userId && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .ToListAsync(cancellationToken);

        var gyms = isSystemAdmin
            ? await dbContext.Gyms
                .Where(gym => gym.IsActive)
                .OrderBy(gym => gym.Name)
                .Select(gym => new WorkspaceGymOption(gym.Code, gym.Name))
                .ToArrayAsync(cancellationToken)
            : links
                .Where(link => link.Gym != null)
                .GroupBy(link => link.Gym!.Code)
                .Select(group => new WorkspaceGymOption(group.Key, group.First().Gym!.Name))
                .ToArray();

        var roles = isSystemAdmin && !string.IsNullOrWhiteSpace(activeGymCode)
            ? new[] { RoleNames.GymOwner, RoleNames.GymAdmin }
            : links
                .Where(link => link.Gym?.Code == activeGymCode)
                .Select(link => link.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new WorkspaceSwitchOptions(gyms, roles);
    }

    private static bool IsSystemAdminTenantRole(string roleName)
    {
        return string.Equals(roleName, RoleNames.GymOwner, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, RoleNames.GymAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
