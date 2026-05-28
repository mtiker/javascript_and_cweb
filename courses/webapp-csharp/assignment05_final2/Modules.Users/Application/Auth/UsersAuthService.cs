using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.Identity;

namespace Modules.Users.Application.Auth;

/// <summary>
/// Phase 4 facade that delegates to Users-owned
/// <see cref="IIdentityService"/> and <see cref="IAccountAuthService"/>.
/// Keeps the module's outward API shape stable.
/// </summary>
internal sealed class UsersAuthService(
    IIdentityService identityService,
    IAccountAuthService accountAuthService) : IUsersAuthService
{
    public Task<JwtResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        => identityService.RegisterAsync(request, cancellationToken);

    public Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => accountAuthService.LoginAsync(request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default)
        => accountAuthService.LogoutAsync(cancellationToken);

    public Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        => accountAuthService.RenewRefreshTokenAsync(request, cancellationToken);

    public Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken = default)
        => identityService.SwitchGymAsync(request, cancellationToken);

    public Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken = default)
        => identityService.SwitchRoleAsync(request, cancellationToken);

    public Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        => identityService.ForgotPasswordAsync(request, cancellationToken);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        => identityService.ResetPasswordAsync(request, cancellationToken);

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        => accountAuthService.ChangeOwnPasswordAsync(request, cancellationToken);
}
