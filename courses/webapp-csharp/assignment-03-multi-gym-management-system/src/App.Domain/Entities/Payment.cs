using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Payment : TenantBaseEntity
{
    public decimal Amount { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    [MaxLength(128)]
    public string? Reference { get; set; }

    public Guid? MembershipId { get; set; }
    public Membership? Membership { get; set; }

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }
}
