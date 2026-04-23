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

public class TokenService(IConfiguration configuration) : ITokenService
{
    public int AccessTokenLifetimeSeconds => (int)TimeSpan.FromMinutes(GetAccessTokenLifetimeMinutes()).TotalSeconds;

    public string CreateJwt(
        AppUser user,
        IReadOnlyCollection<string> systemRoles,
        AppUserGymRole? activeGymRole,
        IReadOnlyCollection<Claim>? additionalClaims = null)
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

        if (additionalClaims is { Count: > 0 })
        {
            claims.AddRange(additionalClaims);
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes());

        var token = new JwtSecurityToken(
            issuer: GetJwtIssuer(),
            audience: GetJwtAudience(),
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
            ValidIssuer = GetJwtIssuer(),
            ValidAudience = GetJwtAudience(),
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
        return configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
    }

    private string GetJwtIssuer()
    {
        return configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
    }

    private string GetJwtAudience()
    {
        return configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        return int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? minutes
            : 60;
    }
}
