namespace App.Domain.Enums;

public enum SubscriptionPlan
{
    Starter = 0,
    Growth = 1,
    Enterprise = 2
}

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}

public enum SupportTicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2
}

public enum SupportTicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

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

public enum ContractStatus
{
    Draft = 0,
    Active = 1,
    Suspended = 2,
    Ended = 3
}

public enum EmployerType
{
    Internal = 0,
    External = 1
}

public enum VacationType
{
    Annual = 0,
    Sick = 1,
    Unpaid = 2,
    Other = 3
}

public enum VacationStatus
{
    Planned = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum TrainingSessionStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Completed = 3
}

public enum ShiftType
{
    Training = 0,
    Assisting = 1
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
    Cancelled = 3
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
