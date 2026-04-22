using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.BLL.Contracts.Infrastructure;
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
