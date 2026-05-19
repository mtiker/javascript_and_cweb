using App.Domain.Entities;

namespace App.BLL.Services;

public sealed record WorkspaceGymOption(string Code, string Name);

public sealed record WorkspaceSwitchOptions(
    IReadOnlyCollection<WorkspaceGymOption> Gyms,
    IReadOnlyCollection<string> RolesInActiveGym);

public interface IWorkspaceContextService
{
    Task<AppUserGymRole?> FindDefaultActiveLinkAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AppUserGymRole?> FindUserGymLinkAsync(Guid userId, string gymCode, CancellationToken cancellationToken = default);

    Task<AppUserGymRole?> FindUserGymRoleLinkAsync(Guid userId, string gymCode, string roleName, CancellationToken cancellationToken = default);

    Task<AppUserGymRole?> BuildSystemAdminGymRoleAsync(Guid userId, string gymCode, string roleName, CancellationToken cancellationToken = default);

    Task<WorkspaceSwitchOptions> GetSwitchOptionsAsync(
        Guid userId,
        bool isSystemAdmin,
        string? activeGymCode,
        CancellationToken cancellationToken = default);
}
