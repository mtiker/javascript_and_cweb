namespace App.DTO.v1.PaymentPlans;

public class PaymentPlanInstallmentResponse
{
    public Guid Id { get; set; }
    public DateTime DueDateUtc { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime? PaidAtUtc { get; set; }
}
