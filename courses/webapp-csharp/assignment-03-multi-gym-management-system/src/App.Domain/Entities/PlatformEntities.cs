using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Gym : BaseEntity
{
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [MaxLength(64)]
    public string Code { get; set; } = default!;

    [MaxLength(64)]
    public string? RegistrationCode { get; set; }

    [MaxLength(128)]
    public string AddressLine { get; set; } = default!;

    [MaxLength(64)]
    public string City { get; set; } = default!;

    [MaxLength(32)]
    public string PostalCode { get; set; } = default!;

    [MaxLength(64)]
    public string Country { get; set; } = "Estonia";

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    public GymSettings? Settings { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<GymContact> Contacts { get; set; } = new List<GymContact>();
    public ICollection<AppUserGymRole> UserRoles { get; set; } = new List<AppUserGymRole>();
}

public class GymSettings : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    [MaxLength(64)]
    public string CurrencyCode { get; set; } = "EUR";

    [MaxLength(64)]
    public string TimeZone { get; set; } = "Europe/Tallinn";

    public bool AllowNonMemberBookings { get; set; } = true;
    public int BookingCancellationHours { get; set; } = 6;

    [Column(TypeName = "jsonb")]
    public LangStr PublicDescription { get; set; } = new("Gym operations workspace", "en");
}

public class Subscription : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Starter;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public decimal MonthlyPrice { get; set; } = 49m;

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";
}

public class SupportTicket : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    [MaxLength(128)]
    public string Title { get; set; } = default!;

    [MaxLength(2000)]
    public string Description { get; set; } = default!;

    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Medium;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? GymId { get; set; }

    [MaxLength(128)]
    public string EntityName { get; set; } = default!;

    public Guid? EntityId { get; set; }

    [MaxLength(64)]
    public string Action { get; set; } = default!;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ChangesJson { get; set; }
}

public class AppUserGymRole : BaseEntity, ITenantEntity
{
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    [MaxLength(64)]
    public string RoleName { get; set; } = default!;

    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
