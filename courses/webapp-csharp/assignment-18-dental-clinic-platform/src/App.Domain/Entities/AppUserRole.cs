using App.Domain.Common;

namespace App.Domain.Entities;

public class AppUserRole : BaseEntity, ITenantEntity
{
    public Guid AppUserId { get; set; }
    public Guid CompanyId { get; set; }
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public App.Domain.Identity.AppUser? AppUser { get; set; }
    public Company? Company { get; set; }
}
