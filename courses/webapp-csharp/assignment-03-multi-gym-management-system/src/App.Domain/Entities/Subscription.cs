using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Subscription : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Starter;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public decimal MonthlyPrice { get; set; } = 49m;

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";
}
