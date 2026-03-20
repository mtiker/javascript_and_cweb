using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.PaymentPlans;

public class PaymentPlanInstallmentRequest
{
    [Required]
    public DateTime DueDateUtc { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }
}
