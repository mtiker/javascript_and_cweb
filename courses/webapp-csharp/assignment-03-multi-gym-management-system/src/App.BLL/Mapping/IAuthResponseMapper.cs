using App.Domain.Entities;
using App.Domain.Identity;
using App.DTO.v1.Identity;

namespace App.BLL.Mapping;

public interface IAuthResponseMapper
{
    JwtResponse Map(
        string jwt,
        AppRefreshToken refreshToken,
        int expiresInSeconds,
        AppUserGymRole? activeLink,
        IReadOnlyCollection<AppUserGymRole> tenantLinks,
        IReadOnlyCollection<string> systemRoles);
}
