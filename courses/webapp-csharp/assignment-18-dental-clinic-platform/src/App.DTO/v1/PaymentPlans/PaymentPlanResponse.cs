namespace App.DTO.v1.PaymentPlans;

public class PaymentPlanResponse
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public string Status { get; set; } = default!;
    public string Terms { get; set; } = default!;
    public decimal ScheduledAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public IReadOnlyCollection<PaymentPlanInstallmentResponse> Installments { get; set; } = Array.Empty<PaymentPlanInstallmentResponse>();
}
