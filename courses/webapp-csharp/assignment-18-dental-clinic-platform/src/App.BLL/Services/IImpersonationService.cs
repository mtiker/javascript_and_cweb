using App.BLL.Contracts.Impersonation;

namespace App.BLL.Services;

public interface IImpersonationService
{
    Task<StartImpersonationResult> StartAsync(Guid actorUserId, StartImpersonationCommand command, CancellationToken cancellationToken);
}
