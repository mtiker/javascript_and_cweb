namespace App.Domain.Enums;

public enum ContactType
{
    Email = 0,
    Phone = 1,
    Other = 2
}

public enum MemberStatus
{
    Active = 0,
    Suspended = 1,
    Left = 2
}

public enum StaffStatus
{
    Active = 0,
    Suspended = 1,
    Inactive = 2
}

public enum TrainingSessionStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Completed = 3
}

public enum BookingStatus
{
    Booked = 0,
    Cancelled = 1,
    Attended = 2,
    NoShow = 3
}

public enum MembershipPackageType
{
    Single = 0,
    Monthly = 1,
    Yearly = 2,
    Custom = 3
}

public enum DurationUnit
{
    Day = 0,
    Month = 1,
    Year = 2
}

public enum MembershipStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    Paused = 4,
    Refunded = 5,
    Renewed = 6
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public enum EquipmentType
{
    Cardio = 0,
    Strength = 1,
    Mobility = 2,
    Accessory = 3,
    Other = 4
}

public enum EquipmentStatus
{
    Active = 0,
    Maintenance = 1,
    Broken = 2,
    Decommissioned = 3
}

public enum MaintenanceTaskType
{
    Scheduled = 0,
    Breakdown = 1
}

public enum MaintenancePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum MaintenanceTaskStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2
}
