using App.BLL.Contracts.Finance;

namespace App.BLL.Services;

public interface IFinanceWorkspaceService
{
    Task<FinanceWorkspaceResult> GetWorkspaceAsync(Guid userId, Guid patientId, CancellationToken cancellationToken);
}
