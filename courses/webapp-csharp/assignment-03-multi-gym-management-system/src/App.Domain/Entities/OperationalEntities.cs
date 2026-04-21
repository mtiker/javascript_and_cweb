using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class TrainingCategory : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Training", "en");

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
}

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

public class WorkShift : TenantBaseEntity
{
    public Guid ContractId { get; set; }
    public EmploymentContract? Contract { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftType ShiftType { get; set; }

    public Guid? TrainingSessionId { get; set; }
    public TrainingSession? TrainingSession { get; set; }

    [MaxLength(256)]
    public string? Comment { get; set; }
}

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

public class MembershipPackage : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Package", "en");

    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

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
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

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

public class OpeningHours : TenantBaseEntity
{
    public int Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
}

public class OpeningHoursException : TenantBaseEntity
{
    public DateOnly ExceptionDate { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Reason { get; set; }
}

public class EquipmentModel : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Equipment", "en");

    public EquipmentType Type { get; set; }

    [MaxLength(64)]
    public string? Manufacturer { get; set; }

    public int MaintenanceIntervalDays { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<Equipment> EquipmentItems { get; set; } = new List<Equipment>();
}

public class Equipment : TenantBaseEntity
{
    public Guid EquipmentModelId { get; set; }
    public EquipmentModel? EquipmentModel { get; set; }

    [MaxLength(32)]
    public string? AssetTag { get; set; }

    [MaxLength(64)]
    public string? SerialNumber { get; set; }

    public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Active;
    public DateOnly? CommissionedAt { get; set; }
    public DateOnly? DecommissionedAt { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();
}

public class MaintenanceTask : TenantBaseEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public Staff? AssignedStaff { get; set; }

    public Guid? CreatedByStaffId { get; set; }
    public Staff? CreatedByStaff { get; set; }

    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Open;
    public DateTime? DueAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
