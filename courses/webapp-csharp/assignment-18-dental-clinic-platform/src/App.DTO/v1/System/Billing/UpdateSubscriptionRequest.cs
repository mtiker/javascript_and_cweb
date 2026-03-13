using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System.Billing;

public class UpdateSubscriptionRequest
{
    [Required]
    [MaxLength(32)]
    public string Tier { get; set; } = default!;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = default!;

    [Range(0, int.MaxValue)]
    public int UserLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int EntityLimit { get; set; }
}
