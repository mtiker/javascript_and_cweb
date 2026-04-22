using System.Security.Claims;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.Domain.Security;
using Microsoft.AspNetCore.Identity;
using App.DTO.v1.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class IdentityService(
    IAppDbContext dbContext,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IUserContextService userContextService) : IIdentityService
{
    public async Task<JwtResponse> RegisterAsync(RegisterRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) != null)
        {
            throw new ValidationAppException("A user with this email already exists.");
        }

        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim(),
            Person = person
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }

        AppUserGymRole? activeLink = null;
        var defaultGym = await dbContext.Gyms.OrderBy(entity => entity.Name).FirstOrDefaultAsync();

        if (defaultGym != null)
        {
            var member = new Member
            {
                GymId = defaultGym.Id,
                PersonId = person.Id,
                MemberCode = $"MEM-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = MemberStatus.Active
            };

            activeLink = new AppUserGymRole
            {
                AppUserId = user.Id,
                GymId = defaultGym.Id,
                RoleName = RoleNames.Member,
                IsActive = true
            };

            dbContext.Members.Add(member);
            dbContext.AppUserGymRoles.Add(activeLink);
            await dbContext.SaveChangesAsync();
        }

        return await BuildJwtResponseAsync(user, activeLink);
    }

    public async Task<JwtResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
                   ?? throw new ValidationAppException("Invalid email or password.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ValidationAppException("Invalid email or password.");
        }

        var activeLink = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .FirstOrDefaultAsync();

        return await BuildJwtResponseAsync(user, activeLink);
    }

    public async Task LogoutAsync()
    {
        var context = userContextService.GetCurrent();
        if (!context.UserId.HasValue)
        {
            return;
        }

        var refreshTokens = await dbContext.RefreshTokens.Where(token => token.UserId == context.UserId.Value).ToListAsync();
        dbContext.RefreshTokens.RemoveRange(refreshTokens);
        await dbContext.SaveChangesAsync();
    }

    public async Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.Jwt);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new ValidationAppException("Invalid refresh token request.");
        }

        var refreshToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.UserId == userId && token.RefreshToken == request.RefreshToken);

        if (refreshToken == null || refreshToken.Expiration <= DateTime.UtcNow)
        {
            throw new ForbiddenException("Refresh token is invalid or expired.");
        }

        var user = refreshToken.User ?? await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found for the provided refresh token.");

        var activeGymCode = principal.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = principal.FindFirstValue(AppClaimTypes.ActiveRole);
        var activeLink = await GetActiveLinkAsync(user.Id, activeGymCode, activeRole);

        dbContext.RefreshTokens.Remove(refreshToken);
        var replacementToken = tokenService.CreateRefreshToken(user.Id, refreshToken);
        dbContext.RefreshTokens.Add(replacementToken);
        await dbContext.SaveChangesAsync();

        return await BuildJwtResponseAsync(user, activeLink, replacementToken);
    }

    public async Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request)
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
            .FirstOrDefaultAsync();

        if (activeLink == null && context.HasRole(RoleNames.SystemAdmin))
        {
            var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Code == request.GymCode && entity.IsActive);
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

        return await BuildJwtResponseAsync(user, activeLink);
    }

    public async Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request)
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
                link.IsActive);

        if (activeLink == null && context.HasRole(RoleNames.SystemAdmin) && IsSystemAdminTenantRole(request.RoleName))
        {
            var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == context.ActiveGymId.Value && entity.IsActive);
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

        return await BuildJwtResponseAsync(user, activeLink);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ForgotPasswordResponse
            {
                Message = "If the email exists, a reset token has been created for the demo environment."
            };
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return new ForgotPasswordResponse
        {
            Message = "Password reset token generated for the demo environment.",
            ResetToken = token
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
                   ?? throw new NotFoundException("User not found.");

        var result = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }
    }

    private async Task<JwtResponse> BuildJwtResponseAsync(AppUser user, AppUserGymRole? activeLink, AppRefreshToken? explicitRefreshToken = null)
    {
        activeLink ??= await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .FirstOrDefaultAsync();

        var systemRoles = (await userManager.GetRolesAsync(user))
            .Where(RoleNames.SystemRoles.Contains)
            .ToArray();

        var jwt = tokenService.CreateJwt(user, systemRoles, activeLink);
        var refreshToken = explicitRefreshToken ?? tokenService.CreateRefreshToken(user.Id);

        if (explicitRefreshToken == null)
        {
            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();
        }

        return new JwtResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken.RefreshToken,
            ExpiresInSeconds = tokenService.AccessTokenLifetimeSeconds,
            ActiveGymId = activeLink?.GymId,
            ActiveGymCode = activeLink?.Gym?.Code,
            ActiveRole = activeLink?.RoleName,
            SystemRoles = systemRoles
        };
    }

    private async Task<AppUserGymRole?> GetActiveLinkAsync(Guid userId, string? gymCode, string? roleName)
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

        var activeLink = await query.OrderBy(link => link.Gym!.Name).ThenBy(link => link.RoleName).FirstOrDefaultAsync();
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

        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Code == gymCode && entity.IsActive);
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
