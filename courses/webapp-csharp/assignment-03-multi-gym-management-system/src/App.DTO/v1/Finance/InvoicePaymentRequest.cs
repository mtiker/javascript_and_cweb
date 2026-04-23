namespace App.DTO.v1.Finance;

public class InvoicePaymentRequest
{
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
