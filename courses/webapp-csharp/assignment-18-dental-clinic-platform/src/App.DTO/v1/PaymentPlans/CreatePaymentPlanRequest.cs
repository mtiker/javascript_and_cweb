using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.PaymentPlans;

public class CreatePaymentPlanRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    public DateTime StartsAtUtc { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Terms { get; set; } = default!;

    [Required]
    [MinLength(1)]
    public List<PaymentPlanInstallmentRequest> Installments { get; set; } = [];
}
