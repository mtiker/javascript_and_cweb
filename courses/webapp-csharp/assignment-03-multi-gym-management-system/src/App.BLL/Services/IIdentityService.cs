using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Identity;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface IIdentityService
{
    Task<JwtResponse> RegisterAsync(RegisterRequest request);
    Task<JwtResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request);
    Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request);
    Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
