using App.BLL.Contracts.Finance;

namespace App.BLL.Services;

public interface ICostEstimateService
{
    Task<IReadOnlyCollection<CostEstimateResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken);
    Task<CostEstimateResult> CreateAsync(Guid userId, CreateCostEstimateCommand command, CancellationToken cancellationToken);
    Task<LegalEstimateResult> GetLegalAsync(Guid userId, Guid costEstimateId, string? countryCode, CancellationToken cancellationToken);
}
