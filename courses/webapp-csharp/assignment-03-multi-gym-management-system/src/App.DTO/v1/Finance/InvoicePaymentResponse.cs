namespace App.DTO.v1.Finance;

public class InvoicePaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public bool IsRefund { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
