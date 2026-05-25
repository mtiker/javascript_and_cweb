namespace App.BLL.Contracts.Services.Admin;

public interface IAdminSessionsQueryService
{
    Task<IReadOnlyList<AdminSessionRow>> GetSessionsAsync(Guid gymId, CancellationToken cancellationToken = default);
}
