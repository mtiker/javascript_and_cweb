using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class TrainingSession : TenantBaseEntity
{
    public Guid CategoryId { get; set; }
    public TrainingCategory? Category { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Session", "en");

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public TrainingSessionStatus Status { get; set; } = TrainingSessionStatus.Draft;
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
