using App.Domain.Enums;

namespace App.DTO.v1.Payments;

public class PaymentCreateRequest
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Reference { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
}
