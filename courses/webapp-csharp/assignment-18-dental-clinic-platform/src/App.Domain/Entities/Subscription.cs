using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Subscription : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAtUtc { get; set; }
    public int UserLimit { get; set; } = 5;
    public int EntityLimit { get; set; } = 100;

    public Company? Company { get; set; }
}
