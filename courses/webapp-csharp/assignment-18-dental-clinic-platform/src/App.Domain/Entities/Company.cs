using App.Domain.Common;

namespace App.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAtUtc { get; set; }

    public CompanySettings? Settings { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
}
