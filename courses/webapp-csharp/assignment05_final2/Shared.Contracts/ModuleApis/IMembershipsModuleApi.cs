namespace Shared.Contracts.ModuleApis;

/// <summary>
/// The Memberships module's outward-facing contract — the only way other
/// modules should look up tenant member identity. Concrete implementation
/// lives in <c>Modules.Memberships.Application</c>. Returns shared DTOs,
/// never EF/domain entities (see Phase 6 constraint). Phase 7 (Training)
/// will consume this to validate Booking → Member relationships without
/// importing the legacy <c>App.Domain</c> entity types.
/// </summary>
public interface IMembershipsModuleApi
{
    /// <summary>
    /// Resolves a member by gym + id. Returns <c>null</c> when the member does
    /// not exist on that gym (or has been soft-deleted).
    /// </summary>
    Task<MemberSummary?> GetMemberSummaryAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the member record (if any) that belongs to the given identity
    /// user on the given gym. Returns <c>null</c> when the user does not have
    /// a member profile on that gym.
    /// </summary>
    Task<MemberSummary?> FindMemberForUserAsync(
        Guid gymId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
