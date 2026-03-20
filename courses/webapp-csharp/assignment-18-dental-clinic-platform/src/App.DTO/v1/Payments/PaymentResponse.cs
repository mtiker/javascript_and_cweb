namespace App.DTO.v1.Payments;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string Method { get; set; } = default!;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
