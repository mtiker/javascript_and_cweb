using App.BLL.Contracts.CompanySettings;

namespace App.BLL.Services;

public interface ICompanySettingsService
{
    Task<CompanySettingsResult> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<CompanySettingsResult> UpdateAsync(Guid userId, UpdateCompanySettingsCommand command, CancellationToken cancellationToken);
}
