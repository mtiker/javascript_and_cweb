namespace App.DTO.v1.Subscriptions;

public class TenantSubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Tier { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int UserLimit { get; set; }
    public int EntityLimit { get; set; }
}
