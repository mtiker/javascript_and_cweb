using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Shared.Contracts.ModuleApis;

namespace Modules.Users.Application;

/// <summary>
/// Phase 4 implementation of <see cref="IUsersModuleApi"/>. Wraps ASP.NET
/// Identity's <see cref="UserManager{TUser}"/> so other modules can resolve
/// user identity without depending on <c>App.Domain</c> or <c>AppDbContext</c>.
/// Returns <see cref="UserSummary"/> projections only — never the underlying
/// <see cref="AppUser"/> entity.
/// </summary>
internal sealed class UsersModuleApiService(UserManager<AppUser> userManager) : IUsersModuleApi
{
    public async Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ProjectAsync(user);
    }

    public async Task<UserSummary?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await ProjectAsync(user);
    }

    private async Task<UserSummary> ProjectAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserSummary(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());
    }
}
