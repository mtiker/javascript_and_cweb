namespace Shared.Contracts.Dtos.v1.Payments;

public class PaymentRefundRequest
{
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
}
