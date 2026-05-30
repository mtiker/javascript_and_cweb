using App.Domain.Entities;

namespace Modules.Gyms.Application.Persistence;

public interface IGymsTenantRepository
{
    Task<GymSettings?> FindGymSettingsAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppUserGymRole>> ListGymUsersAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<AppUserGymRole?> FindGymUserRoleAsync(Guid gymId, Guid appUserId, string roleName, CancellationToken cancellationToken = default);
    Task AddGymUserRoleAsync(AppUserGymRole entity, CancellationToken cancellationToken = default);
    void RemoveGymUserRole(AppUserGymRole entity);
}
