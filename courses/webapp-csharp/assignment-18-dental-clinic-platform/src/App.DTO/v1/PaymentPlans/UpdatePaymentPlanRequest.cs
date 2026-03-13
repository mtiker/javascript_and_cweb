using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.PaymentPlans;

public class UpdatePaymentPlanRequest
{
    [Range(1, 120)]
    public int InstallmentCount { get; set; }

    [Range(0, 999999999)]
    public decimal InstallmentAmount { get; set; }

    [Required]
    public DateTime StartsAtUtc { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = default!;

    [Required]
    [MaxLength(2000)]
    public string Terms { get; set; } = default!;
}
