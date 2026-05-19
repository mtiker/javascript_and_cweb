namespace App.BLL.Contracts.Services.Client;

public interface IClientDashboardQueryService
{
    Task<ClientDashboardSnapshot> GetSnapshotAsync(
        Guid gymId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default);
}
