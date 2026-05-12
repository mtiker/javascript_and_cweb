using System.Security.Claims;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using App.Domain.Security;
using App.DTO.v1;
using App.DTO.v1.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Modules.Users.Application.Auth;

internal interface IUsersSessionService
{
    Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Message> LogoutAsync(CancellationToken cancellationToken);

    Task<JwtResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken);

    Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken);
}

internal sealed class UsersSessionService(
    IAppDbContext dbContext,
    IAppUnitOfWork unitOfWork,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IUserContextService userContextService,
    IAuthResponseMapper authResponseMapper) : IUsersSessionService
{
    public async Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
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

    public async Task<Message> LogoutAsync(CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue)
        {
            return new Message("Logged out.");
        }

        var refreshTokens = await unitOfWork.RefreshTokens.ListByUserAsync(context.UserId.Value, cancellationToken);
        unitOfWork.RefreshTokens.RemoveRange(refreshTokens);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new Message("Logged out.");
    }

    public async Task<JwtResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
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

        var refreshToken = await unitOfWork.RefreshTokens.GetByUserAndTokenAsync(userId, request.RefreshToken, cancellationToken);
        if (refreshToken == null || refreshToken.Expiration <= DateTime.UtcNow)
        {
            throw new ForbiddenException("Refresh token is invalid or expired.");
        }

        var user = refreshToken.User ?? await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found for the provided refresh token.");

        var activeGymCode = principal.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = principal.FindFirstValue(AppClaimTypes.ActiveRole);
        var activeLink = await GetActiveLinkAsync(user.Id, activeGymCode, activeRole, cancellationToken);

        unitOfWork.RefreshTokens.Remove(refreshToken);
        var replacementToken = tokenService.CreateRefreshToken(user.Id, refreshToken);
        await unitOfWork.RefreshTokens.AddAsync(replacementToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildJwtResponseAsync(user, activeLink, replacementToken, cancellationToken);
    }

    public async Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var user = await userManager.FindByIdAsync(context.UserId.Value.ToString())
                   ?? throw new NotFoundException("User not found.");

        var activeLink = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive && link.Gym!.Code == request.GymCode)
            .OrderBy(link => link.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeLink == null && context.HasRole(RoleNames.SystemAdmin))
        {
            var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Code == request.GymCode && entity.IsActive, cancellationToken);
            if (gym != null)
            {
                activeLink = new AppUserGymRole
                {
                    AppUserId = user.Id,
                    GymId = gym.Id,
                    Gym = gym,
                    RoleName = RoleNames.GymOwner,
                    IsActive = true
                };
            }
        }

        if (activeLink == null)
        {
            throw new ForbiddenException("The user does not have access to the requested gym.");
        }

        return await BuildJwtResponseAsync(user, activeLink, cancellationToken: cancellationToken);
    }

    public async Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue || !context.ActiveGymId.HasValue)
        {
            throw new ForbiddenException("An active gym is required to switch tenant roles.");
        }

        var user = await userManager.FindByIdAsync(context.UserId.Value.ToString())
                   ?? throw new NotFoundException("User not found.");

        var activeLink = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .FirstOrDefaultAsync(link =>
                link.AppUserId == user.Id &&
                link.GymId == context.ActiveGymId.Value &&
                link.RoleName == request.RoleName &&
                link.IsActive,
                cancellationToken);

        if (activeLink == null && context.HasRole(RoleNames.SystemAdmin) && IsSystemAdminTenantRole(request.RoleName))
        {
            var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == context.ActiveGymId.Value && entity.IsActive, cancellationToken);
            if (gym != null)
            {
                activeLink = new AppUserGymRole
                {
                    AppUserId = user.Id,
                    GymId = gym.Id,
                    Gym = gym,
                    RoleName = request.RoleName,
                    IsActive = true
                };
            }
        }

        if (activeLink == null)
        {
            throw new ForbiddenException("The requested role is not assigned in the active gym.");
        }

        return await BuildJwtResponseAsync(user, activeLink, cancellationToken: cancellationToken);
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
            await unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
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

    private async Task<AppUserGymRole?> GetActiveLinkAsync(Guid userId, string? gymCode, string? roleName, CancellationToken cancellationToken)
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
