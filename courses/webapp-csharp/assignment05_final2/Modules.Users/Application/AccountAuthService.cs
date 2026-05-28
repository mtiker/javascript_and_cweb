using App.BLL.Contracts.Services;
using System.Security.Claims;
using App.BLL.Contracts.Infrastructure;
using SharedKernel.Exceptions;
using Modules.Users.Application.Mappers;
using SharedKernel;
using App.Domain.Entities;
using App.Domain.Identity;
using SharedKernel.Security;
using Shared.Contracts.Dtos.v1.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modules.Users.Application.Persistence;

namespace Modules.Users.Application;

public sealed class AccountAuthService(
    IAppDbContext dbContext,
    IRefreshTokenRepository refreshTokens,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IUserContextService userContextService,
    IAuthResponseMapper authResponseMapper) : IAccountAuthService
{
    public async Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
                   ?? throw new ValidationAppException("Invalid email or password.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ValidationAppException("Invalid email or password.");
        }

        var activeLink = await GetDefaultActiveLinkAsync(user.Id, cancellationToken);

        return await BuildJwtResponseAsync(user, activeLink, cancellationToken: cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue)
        {
            return;
        }

        var userRefreshTokens = await refreshTokens.ListByUserAsync(context.UserId.Value, cancellationToken);
        refreshTokens.RemoveRange(userRefreshTokens);
        await refreshTokens.SaveChangesAsync(cancellationToken);
    }

    public async Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = tokenService.GetPrincipalFromExpiredToken(request.Jwt);
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            throw new ValidationAppException("Invalid refresh token request.");
        }

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new ValidationAppException("Invalid refresh token request.");
        }

        var refreshToken = await refreshTokens.GetByUserAndTokenAsync(userId, request.RefreshToken, cancellationToken);
        if (refreshToken == null || refreshToken.Expiration <= DateTime.UtcNow)
        {
            throw new ForbiddenException("Refresh token is invalid or expired.");
        }

        var user = refreshToken.User ?? await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found for the provided refresh token.");

        var activeGymCode = principal.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = principal.FindFirstValue(AppClaimTypes.ActiveRole);
        var activeLink = await GetActiveLinkAsync(user.Id, activeGymCode, activeRole, cancellationToken);

        refreshTokens.Remove(refreshToken);
        var replacementToken = tokenService.CreateRefreshToken(user.Id, refreshToken);
        await refreshTokens.AddAsync(replacementToken, cancellationToken);
        await refreshTokens.SaveChangesAsync(cancellationToken);

        return await BuildJwtResponseAsync(user, activeLink, replacementToken, cancellationToken);
    }

    public async Task ChangeOwnPasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationAppException("New password is required.");
        }

        var user = await userManager.FindByIdAsync(context.UserId.Value.ToString())
                   ?? throw new NotFoundException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }
    }

    private async Task<JwtResponse> BuildJwtResponseAsync(
        AppUser user,
        AppUserGymRole? activeLink,
        AppRefreshToken? explicitRefreshToken = null,
        CancellationToken cancellationToken = default)
    {
        activeLink ??= await GetDefaultActiveLinkAsync(user.Id, cancellationToken);

        var systemRoles = (await userManager.GetRolesAsync(user))
            .Where(RoleNames.SystemRoles.Contains)
            .ToArray();

        var tenantLinks = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive && link.Gym != null && link.Gym.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .ToListAsync(cancellationToken);

        var jwt = tokenService.CreateJwt(user, systemRoles, activeLink);
        var refreshToken = explicitRefreshToken ?? tokenService.CreateRefreshToken(user.Id);

        if (explicitRefreshToken == null)
        {
            await refreshTokens.AddAsync(refreshToken, cancellationToken);
            await refreshTokens.SaveChangesAsync(cancellationToken);
        }

        return authResponseMapper.Map(
            jwt,
            refreshToken,
            tokenService.AccessTokenLifetimeSeconds,
            activeLink,
            tenantLinks,
            systemRoles);
    }

    private async Task<AppUserGymRole?> GetDefaultActiveLinkAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == userId && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AppUserGymRole?> GetActiveLinkAsync(Guid userId, string? gymCode, string? roleName, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == userId && link.IsActive);

        if (!string.IsNullOrWhiteSpace(gymCode))
        {
            query = query.Where(link => link.Gym!.Code == gymCode);
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            query = query.Where(link => link.RoleName == roleName);
        }

        var activeLink = await query.OrderBy(link => link.Gym!.Name).ThenBy(link => link.RoleName).FirstOrDefaultAsync(cancellationToken);
        if (activeLink != null)
        {
            return activeLink;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null ||
            !await userManager.IsInRoleAsync(user, RoleNames.SystemAdmin) ||
            string.IsNullOrWhiteSpace(gymCode) ||
            string.IsNullOrWhiteSpace(roleName) ||
            !IsSystemAdminTenantRole(roleName))
        {
            return null;
        }

        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Code == gymCode && entity.IsActive, cancellationToken);
        return gym == null
            ? null
            : new AppUserGymRole
            {
                AppUserId = userId,
                GymId = gym.Id,
                Gym = gym,
                RoleName = roleName,
                IsActive = true
            };
    }

    private static bool IsSystemAdminTenantRole(string roleName)
    {
        return string.Equals(roleName, RoleNames.GymOwner, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, RoleNames.GymAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
