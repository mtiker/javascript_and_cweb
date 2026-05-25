namespace App.BLL.Contracts.Services.Admin;

public interface IAdminOperationsQueryService
{
    Task<AdminOperationsSnapshot> GetSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default);
}
