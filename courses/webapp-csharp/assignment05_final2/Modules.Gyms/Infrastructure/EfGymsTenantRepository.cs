using App.DAL.EF;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.Gyms.Application.Persistence;

namespace Modules.Gyms.Infrastructure;

internal sealed class EfGymsTenantRepository(AppDbContext dbContext) : IGymsTenantRepository
{
    public Task<GymSettings?> FindGymSettingsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return dbContext.GymSettings.FirstOrDefaultAsync(entity => entity.GymId == gymId, cancellationToken);
    }

    public async Task<IReadOnlyList<AppUserGymRole>> ListGymUsersAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AppUserGymRoles
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.RoleName)
            .ToListAsync(cancellationToken);
    }

    public Task<AppUserGymRole?> FindGymUserRoleAsync(Guid gymId, Guid appUserId, string roleName, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUserGymRoles.FirstOrDefaultAsync(
            entity => entity.GymId == gymId && entity.AppUserId == appUserId && entity.RoleName == roleName,
            cancellationToken);
    }

    public async Task AddGymUserRoleAsync(AppUserGymRole entity, CancellationToken cancellationToken = default)
    {
        await dbContext.AppUserGymRoles.AddAsync(entity, cancellationToken);
    }

    public void RemoveGymUserRole(AppUserGymRole entity)
    {
        dbContext.AppUserGymRoles.Remove(entity);
    }
}
