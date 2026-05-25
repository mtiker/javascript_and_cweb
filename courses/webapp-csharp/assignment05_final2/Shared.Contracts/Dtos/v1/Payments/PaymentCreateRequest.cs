using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Payments;

public class PaymentCreateRequest
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Reference { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
}
