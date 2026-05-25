using App.Domain.Entities;
using App.Domain.Identity;
using Shared.Contracts.Dtos.v1.Identity;

namespace Modules.Users.Application.Mappers;

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
