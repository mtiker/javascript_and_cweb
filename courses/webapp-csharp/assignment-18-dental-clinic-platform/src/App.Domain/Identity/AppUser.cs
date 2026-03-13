using App.Domain.Common;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<AppRefreshToken> RefreshTokens { get; set; } = new List<AppRefreshToken>();
    public ICollection<AppUserRole> CompanyRoles { get; set; } = new List<AppUserRole>();
}
