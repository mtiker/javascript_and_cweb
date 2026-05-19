using App.Domain.Entities;
using App.Domain.Identity;
using App.DTO.v1.Identity;

namespace App.BLL.Mappers;

public sealed class AuthResponseMapper : IAuthResponseMapper
{
    public JwtResponse Map(
        string jwt,
        AppRefreshToken refreshToken,
        int expiresInSeconds,
        AppUserGymRole? activeLink,
        IReadOnlyCollection<AppUserGymRole> tenantLinks,
        IReadOnlyCollection<string> systemRoles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);
        ArgumentNullException.ThrowIfNull(refreshToken);
        ArgumentNullException.ThrowIfNull(tenantLinks);
        ArgumentNullException.ThrowIfNull(systemRoles);

        var availableTenants = tenantLinks
            .Where(link => link.Gym != null)
            .GroupBy(link => new
            {
                link.GymId,
                link.Gym!.Code,
                link.Gym.Name
            })
            .Select(group => new TenantAccessResponse
            {
                GymId = group.Key.GymId,
                GymCode = group.Key.Code,
                GymName = group.Key.Name,
                Roles = group
                    .Select(link => link.RoleName)
                    .Distinct()
                    .OrderBy(role => role)
                    .ToArray()
            })
            .OrderBy(tenant => tenant.GymName)
            .ToArray();

        return new JwtResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken.RefreshToken,
            ExpiresInSeconds = expiresInSeconds,
            ActiveGymId = activeLink?.GymId,
            ActiveGymCode = activeLink?.Gym?.Code,
            ActiveRole = activeLink?.RoleName,
            SystemRoles = systemRoles.ToArray(),
            AvailableTenants = availableTenants
        };
    }
}
