namespace Shared.Contracts.ModuleApis;

/// <summary>
/// The Users module's outward-facing contract — the only way other modules
/// should look up identity information. Concrete implementation lives in
/// <c>Modules.Users.Application</c>. Returns shared DTOs, never EF/domain
/// entities (see Phase 4 constraint).
/// </summary>
public interface IUsersModuleApi
{
    /// <summary>
    /// Resolves a user by id. Returns <c>null</c> when the user does not exist.
    /// </summary>
    Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a user by email (case-insensitive). Returns <c>null</c> when
    /// no user matches.
    /// </summary>
    Task<UserSummary?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}
