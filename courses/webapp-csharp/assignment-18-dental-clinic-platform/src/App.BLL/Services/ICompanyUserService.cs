using App.BLL.Contracts.CompanyUsers;

namespace App.BLL.Services;

public interface ICompanyUserService
{
    Task<IReadOnlyCollection<CompanyUserResult>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<CompanyUserResult> UpsertAsync(Guid actorUserId, UpsertCompanyUserCommand command, CancellationToken cancellationToken);
}
