namespace App.BLL.Services.Client;

public interface IClientDashboardQueryService
{
    Task<ClientDashboardSnapshot> GetSnapshotAsync(
        Guid gymId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default);
}
