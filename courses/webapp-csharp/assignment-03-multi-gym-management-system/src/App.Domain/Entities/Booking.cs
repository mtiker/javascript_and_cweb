using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Booking : TenantBaseEntity
{
    public Guid TrainingSessionId { get; set; }
    public TrainingSession? TrainingSession { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public DateTime BookedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAtUtc { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Booked;
    public decimal ChargedPrice { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public bool PaymentRequired { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
