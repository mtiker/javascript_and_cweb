using Shared.Contracts.Dtos.v1.Identity;

namespace App.BLL.Contracts.Services;

public interface IAccountAuthService
{
    Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
