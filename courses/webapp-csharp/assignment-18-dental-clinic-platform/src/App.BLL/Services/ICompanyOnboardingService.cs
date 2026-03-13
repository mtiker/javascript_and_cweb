using App.BLL.Contracts;

namespace App.BLL.Services;

public interface ICompanyOnboardingService
{
    Task<RegisterCompanyResult> RegisterCompanyAsync(RegisterCompanyCommand command, CancellationToken cancellationToken);
}
