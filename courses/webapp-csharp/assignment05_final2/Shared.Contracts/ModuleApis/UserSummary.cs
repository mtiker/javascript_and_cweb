namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of an identity user. Crosses module boundaries via
/// <see cref="IUsersModuleApi"/>; never expose the EF/domain
/// <c>AppUser</c> entity to other modules.
/// </summary>
public sealed record UserSummary(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> SystemRoles);
