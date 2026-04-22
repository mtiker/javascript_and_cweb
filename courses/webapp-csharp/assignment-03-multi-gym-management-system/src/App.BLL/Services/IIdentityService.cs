using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Identity;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface IIdentityService
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
