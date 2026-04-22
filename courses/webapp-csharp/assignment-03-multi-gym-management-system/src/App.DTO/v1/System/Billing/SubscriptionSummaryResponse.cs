using App.Domain.Enums;

namespace App.DTO.v1.System.Billing;

public class SubscriptionSummaryResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public decimal MonthlyPrice { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
