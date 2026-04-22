using App.Domain.Enums;

namespace App.DTO.v1.System.Billing;

public class UpdateSubscriptionRequest
{
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal MonthlyPrice { get; set; }
}
