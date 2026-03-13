using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Subscriptions;

public class UpdateTenantSubscriptionRequest
{
    [Required]
    [MaxLength(32)]
    public string Tier { get; set; } = default!;
}
