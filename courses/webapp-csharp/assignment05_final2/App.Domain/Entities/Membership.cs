using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Common;
using Base.Domain;
using Shared.Contracts.Enums;

namespace App.Domain.Entities;

public class Membership : TenantBaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid MembershipPackageId { get; set; }
    public MembershipPackage? MembershipPackage { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal PriceAtPurchase { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

    // Incremented by the Memberships module when Training publishes
    // BookingConfirmedNotification, so the Members module can later expose
    // consumption against this membership package.
    public int SessionsConsumed { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
