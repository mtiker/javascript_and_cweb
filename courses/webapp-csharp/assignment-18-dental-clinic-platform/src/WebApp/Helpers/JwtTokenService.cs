using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using App.Domain.Identity;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.Helpers;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private const string SettingsPrefix = "JWT";
    private readonly string _key = configuration.GetValue<string>($"{SettingsPrefix}:Key")
                                   ?? throw new InvalidOperationException("JWT:Key is missing.");
    private readonly string _issuer = configuration.GetValue<string>($"{SettingsPrefix}:Issuer")
                                      ?? throw new InvalidOperationException("JWT:Issuer is missing.");
    private readonly string _audience = configuration.GetValue<string>($"{SettingsPrefix}:Audience")
                                        ?? throw new InvalidOperationException("JWT:Audience is missing.");

    public int ExpiresInSeconds => configuration.GetValue<int>($"{SettingsPrefix}:ExpiresInSeconds", 900);
    public int RefreshTokenExpiresInSeconds => configuration.GetValue<int>($"{SettingsPrefix}:RefreshTokenExpiresInSeconds", 604800);

    public string GenerateToken(
        AppUser user,
        IEnumerable<string> globalRoles,
        Guid? companyId,
        string? companySlug,
        string? companyRole,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString())
        };

        foreach (var role in globalRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (companyId.HasValue)
        {
            claims.Add(new Claim("companyId", companyId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(companySlug))
        {
            claims.Add(new Claim("companySlug", companySlug));
        }

        if (!string.IsNullOrWhiteSpace(companyRole))
        {
            claims.Add(new Claim("companyRole", companyRole));
            claims.Add(new Claim(ClaimTypes.Role, companyRole));
        }

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(ExpiresInSeconds),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryReadPrincipalWithoutLifetimeValidation(string jwt, out ClaimsPrincipal? principal)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = false
        };

        try
        {
            principal = new JwtSecurityTokenHandler().ValidateToken(jwt, tokenValidationParameters, out _);
            return true;
        }
        catch
        {
            principal = null;
            return false;
        }
    }
}
