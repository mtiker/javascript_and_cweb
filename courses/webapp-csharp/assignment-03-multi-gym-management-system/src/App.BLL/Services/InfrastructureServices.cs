using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.Domain.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace App.BLL.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public UserExecutionContext GetCurrent()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new UserExecutionContext(null, null, null, null, null, [], []);
        }

        var userId = TryParseGuid(principal.FindFirstValue(ClaimTypes.NameIdentifier));
        var personId = TryParseGuid(principal.FindFirstValue(AppClaimTypes.PersonId));
        var activeGymId = TryParseGuid(principal.FindFirstValue(AppClaimTypes.GymId));
        var activeGymCode = principal.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = principal.FindFirstValue(AppClaimTypes.ActiveRole);
        var allRoles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var systemRoles = allRoles.Where(RoleNames.SystemRoles.Contains).ToArray();

        return new UserExecutionContext(userId, personId, activeGymId, activeGymCode, activeRole, allRoles, systemRoles);
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public class TokenService(IConfiguration configuration) : ITokenService
{
    public int AccessTokenLifetimeSeconds => (int)TimeSpan.FromMinutes(GetAccessTokenLifetimeMinutes()).TotalSeconds;

    public string CreateJwt(AppUser user, IReadOnlyCollection<string> systemRoles, AppUserGymRole? activeGymRole)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? user.UserName ?? user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (user.PersonId.HasValue)
        {
            claims.Add(new Claim(AppClaimTypes.PersonId, user.PersonId.Value.ToString()));
        }

        foreach (var systemRole in systemRoles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, systemRole));
        }

        if (activeGymRole != null)
        {
            claims.Add(new Claim(AppClaimTypes.GymId, activeGymRole.GymId.ToString()));
            claims.Add(new Claim(AppClaimTypes.GymCode, activeGymRole.Gym?.Code ?? string.Empty));
            claims.Add(new Claim(AppClaimTypes.ActiveRole, activeGymRole.RoleName));
            claims.Add(new Claim(ClaimTypes.Role, activeGymRole.RoleName));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes());

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "multi-gym-management-system",
            audience: configuration["Jwt:Audience"] ?? "multi-gym-management-system",
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string jwt)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = configuration["Jwt:Issuer"] ?? "multi-gym-management-system",
            ValidAudience = configuration["Jwt:Audience"] ?? "multi-gym-management-system",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid JWT token.");
        }

        return principal;
    }

    public AppRefreshToken CreateRefreshToken(Guid userId, AppRefreshToken? previousToken = null)
    {
        var refreshValue = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        return new AppRefreshToken
        {
            UserId = userId,
            RefreshToken = refreshValue,
            Expiration = DateTime.UtcNow.AddDays(30),
            PreviousRefreshToken = previousToken?.RefreshToken,
            PreviousExpiration = previousToken?.Expiration
        };
    }

    private string GetJwtKey()
    {
        return configuration["Jwt:Key"] ?? "super-secret-assignment-03-key-change-me";
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        return int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? minutes
            : 60;
    }
}

public class AuthorizationService(
    IAppDbContext dbContext,
    IUserContextService userContextService) : IAuthorizationService
{
    public async Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles)
    {
        var context = userContextService.GetCurrent();
        if (!context.IsAuthenticated || !context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            throw new AppForbiddenException("An active gym context is required.");
        }

        var gym = await dbContext.Gyms.AsNoTracking().FirstOrDefaultAsync(entity => entity.Code == gymCode);
        if (gym == null)
        {
            throw new AppNotFoundException($"Gym '{gymCode}' was not found.");
        }

        if (context.ActiveGymId != gym.Id || !string.Equals(context.ActiveGymCode, gymCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppForbiddenException("The requested gym does not match the active gym context.");
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Any(context.HasRole))
        {
            throw new AppForbiddenException("You do not have permission to access this gym resource.");
        }

        return gym.Id;
    }

    public async Task<Member?> GetCurrentMemberAsync(Guid gymId)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Members.FirstOrDefaultAsync(member => member.GymId == gymId && member.PersonId == context.PersonId);
    }

    public async Task<Staff?> GetCurrentStaffAsync(Guid gymId)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Staff.FirstOrDefaultAsync(staff => staff.GymId == gymId && staff.PersonId == context.PersonId);
    }

    public async Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Member))
        {
            throw new AppForbiddenException("Only gym admins or the owning member can access this resource.");
        }

        var currentMember = await GetCurrentMemberAsync(gymId);
        if (currentMember?.Id != memberId)
        {
            throw new AppForbiddenException("Members can access only their own records.");
        }
    }

    public async Task EnsureBookingAccessAsync(Booking booking)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (context.HasRole(RoleNames.Member))
        {
            await EnsureMemberSelfAccessAsync(booking.GymId, booking.MemberId);
            return;
        }

        if (context.HasRole(RoleNames.Trainer))
        {
            var currentStaff = await GetCurrentStaffAsync(booking.GymId);
            if (currentStaff == null)
            {
                throw new AppForbiddenException("Trainer staff profile not found for the active gym.");
            }

            var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
                shift.GymId == booking.GymId &&
                shift.TrainingSessionId == booking.TrainingSessionId &&
                shift.Contract!.StaffId == currentStaff.Id &&
                shift.ShiftType == ShiftType.Training);

            if (!assigned)
            {
                throw new AppForbiddenException("Trainers can access bookings only for sessions assigned to them.");
            }

            return;
        }

        throw new AppForbiddenException("You do not have permission to access this booking.");
    }

    public async Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Trainer))
        {
            throw new AppForbiddenException("Only assigned trainers or gym admins can update attendance.");
        }

        var currentStaff = await GetCurrentStaffAsync(trainingSession.GymId);
        if (currentStaff == null)
        {
            throw new AppForbiddenException("Trainer staff profile not found for the active gym.");
        }

        var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
            shift.GymId == trainingSession.GymId &&
            shift.TrainingSessionId == trainingSession.Id &&
            shift.Contract!.StaffId == currentStaff.Id &&
            shift.ShiftType == ShiftType.Training);

        if (!assigned)
        {
            throw new AppForbiddenException("Only assigned trainers can update session attendance.");
        }
    }

    public async Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Caretaker))
        {
            throw new AppForbiddenException("Only assigned caretakers or gym admins can manage maintenance tasks.");
        }

        var currentStaff = await GetCurrentStaffAsync(task.GymId);
        if (currentStaff == null || task.AssignedStaffId != currentStaff.Id)
        {
            throw new AppForbiddenException("Caretakers can update only tasks assigned to them.");
        }
    }

    private static bool HasTenantAdminPrivileges(UserExecutionContext context)
    {
        return context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin);
    }
}
