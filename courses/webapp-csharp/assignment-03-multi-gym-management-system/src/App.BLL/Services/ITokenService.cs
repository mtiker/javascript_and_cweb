using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface ITokenService
{
    string CreateJwt(AppUser user, IReadOnlyCollection<string> systemRoles, AppUserGymRole? activeGymRole);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string jwt);
    AppRefreshToken CreateRefreshToken(Guid userId, AppRefreshToken? previousToken = null);
    int AccessTokenLifetimeSeconds { get; }
}
