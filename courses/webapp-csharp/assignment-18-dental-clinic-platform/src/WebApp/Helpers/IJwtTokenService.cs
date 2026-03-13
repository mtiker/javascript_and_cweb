using System.Security.Claims;
using App.Domain.Identity;

namespace WebApp.Helpers;

public interface IJwtTokenService
{
    string GenerateToken(
        AppUser user,
        IEnumerable<string> globalRoles,
        Guid? companyId,
        string? companySlug,
        string? companyRole,
        IEnumerable<Claim>? additionalClaims = null);

    bool TryReadPrincipalWithoutLifetimeValidation(string jwt, out ClaimsPrincipal? principal);
    int ExpiresInSeconds { get; }
    int RefreshTokenExpiresInSeconds { get; }
}
