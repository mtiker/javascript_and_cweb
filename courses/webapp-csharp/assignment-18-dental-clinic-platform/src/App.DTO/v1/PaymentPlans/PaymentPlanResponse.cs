namespace App.DTO.v1.PaymentPlans;

public class PaymentPlanResponse
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public string Status { get; set; } = default!;
    public string Terms { get; set; } = default!;
}
