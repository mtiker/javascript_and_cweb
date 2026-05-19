using App.DTO.v1.System.Platform;
using App.DTO.v1.System;

namespace App.BLL.Contracts.Services;

public interface IPlatformService
{
    Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync(CancellationToken cancellationToken = default);
    Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request, CancellationToken cancellationToken = default);
    Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request, CancellationToken cancellationToken = default);
    Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<PlatformAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default);
}
