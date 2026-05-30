using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;

namespace Modules.Gyms.Application;

public interface IGymsTenantWorkflowService
{
    Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default);
}
