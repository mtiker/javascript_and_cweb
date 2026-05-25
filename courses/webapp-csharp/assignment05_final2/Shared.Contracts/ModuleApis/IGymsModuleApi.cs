namespace Shared.Contracts.ModuleApis;

/// <summary>
/// The Gyms module's outward-facing contract — the only way other modules
/// should resolve tenant gym access. Concrete implementation lives in
/// <c>Modules.Gyms.Application</c>. Returns shared DTOs, never EF/domain
/// entities (see Phase 5 constraint).
/// </summary>
public interface IGymsModuleApi
{
    /// <summary>
    /// Resolves the active gym + caller role(s) for a tenant request. Returns
    /// <c>null</c> when the gym code does not exist, the gym is inactive, or
    /// the caller has no role on it. When <paramref name="allowedRoles"/> is
    /// non-empty, the caller must hold at least one of them.
    /// </summary>
    Task<GymAccess?> ResolveAccessAsync(
        Guid userId,
        string gymCode,
        IReadOnlyCollection<string>? allowedRoles = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all gyms the given user holds any role on. Empty when the user
    /// has no tenant access.
    /// </summary>
    Task<IReadOnlyList<GymAccess>> ListGymsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<GymSettingsSummary?> GetSettingsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default);
}
