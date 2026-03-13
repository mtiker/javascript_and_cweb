namespace App.DTO.v1.System.Billing;

public class SubscriptionSummaryResponse
{
    public Guid SubscriptionId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string CompanySlug { get; set; } = default!;
    public string Tier { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int UserLimit { get; set; }
    public int EntityLimit { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}
