using App.Domain.Enums;

namespace App.DTO.v1.Payments;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public DateTime PaidAtUtc { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Reference { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
}
