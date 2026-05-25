namespace Modules.Gyms.Application.Persistence;

public interface IAuthorizationQueryRepository
{
    Task<AuthorizationGymLookup?> FindGymByCodeAsync(string gymCode, CancellationToken cancellationToken = default);
}

public sealed record AuthorizationGymLookup(Guid Id, string Code);
