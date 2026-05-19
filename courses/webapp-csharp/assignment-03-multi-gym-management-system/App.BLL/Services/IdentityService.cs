using App.BLL.Infrastructure;
using App.BLL.Exceptions;
using App.BLL.Mappers;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using App.DTO.v1.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class IdentityService(
    IAppDbContext dbContext,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IUserContextService userContextService,
    IAuthResponseMapper authResponseMapper) : IIdentityService
{
    public async Task<JwtResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
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
        var defaultGym = await dbContext.Gyms.OrderBy(entity => entity.Name).FirstOrDefaultAsync(cancellationToken);

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
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await BuildJwtResponseAsync(user, activeLink, cancellationToken: cancellationToken);
    }

    public async Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken = default)
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

    public async Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken = default)
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

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
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

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
                   ?? throw new NotFoundException("User not found.");

        var result = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }
    }

    private async Task<JwtResponse> BuildJwtResponseAsync(AppUser user, AppUserGymRole? activeLink, AppRefreshToken? explicitRefreshToken = null, CancellationToken cancellationToken = default)
    {
        activeLink ??= await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

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
            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return authResponseMapper.Map(
            jwt,
            refreshToken,
            tokenService.AccessTokenLifetimeSeconds,
            activeLink,
            tenantLinks,
            systemRoles);
    }

    private static bool IsSystemAdminTenantRole(string roleName)
    {
        return string.Equals(roleName, RoleNames.GymOwner, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, RoleNames.GymAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
