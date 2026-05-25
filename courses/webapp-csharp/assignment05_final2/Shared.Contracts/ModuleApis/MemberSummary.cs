namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of a member's tenant identity. Crosses module boundaries
/// via <see cref="IMembershipsModuleApi"/>; never expose the EF/domain
/// <c>Member</c> or <c>Person</c> entities to other modules. <see cref="Status"/>
/// uses the enum name (e.g. <c>Active</c>, <c>Suspended</c>) as a string so
/// <c>Shared.Contracts</c> can stay App.Domain-free.
/// </summary>
public sealed record MemberSummary(
    Guid Id,
    Guid GymId,
    string MemberCode,
    string FullName,
    string Status);
