namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of a user's access to a tenant gym. Crosses module
/// boundaries via <see cref="IGymsModuleApi"/>; never expose the EF/domain
/// <c>Gym</c> or <c>AppUserGymRole</c> entities to other modules.
/// </summary>
public sealed record GymAccess(
    Guid GymId,
    string GymCode,
    bool IsActive,
    IReadOnlyList<string> Roles);
