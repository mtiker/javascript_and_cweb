using Shared.Contracts.Dtos.v1.Identity;

namespace Modules.Users.Application.Auth;

/// <summary>
/// Users module's internal application service for all identity/auth
/// operations exposed under <c>api/v1/account</c>. AccountController in
/// <c>Modules.Users.Api</c> talks to this facade rather than binding the
/// controller to lower-level identity services directly.
/// </summary>
public interface IUsersAuthService
{
    Task<JwtResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken = default);

    Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken = default);

    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
