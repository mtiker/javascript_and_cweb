using App.Domain.Common;
using Microsoft.AspNetCore.Identity;
using App.Domain.Entities;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public Guid? PersonId { get; set; }
    public Person? Person { get; set; }
    public string? DisplayName { get; set; }

    public ICollection<AppRefreshToken> RefreshTokens { get; set; } = new List<AppRefreshToken>();
    public ICollection<AppUserGymRole> GymRoles { get; set; } = new List<AppUserGymRole>();
}
