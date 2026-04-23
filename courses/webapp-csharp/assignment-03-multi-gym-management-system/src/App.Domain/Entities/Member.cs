using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Member : TenantBaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    [MaxLength(32)]
    public string MemberCode { get; set; } = default!;

    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateOnly JoinedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? LeftAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<CoachingPlan> CoachingPlans { get; set; } = new List<CoachingPlan>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
