namespace App.Domain.Enums;

public enum SubscriptionTier
{
    Free = 0,
    Standard = 1,
    Premium = 2
}

public enum SubscriptionStatus
{
    Active = 0,
    PastDue = 1,
    Cancelled = 2
}

public enum ToothConditionStatus
{
    Healthy = 0,
    Caries = 1,
    Filled = 2,
    Crown = 3,
    RootCanal = 4,
    Missing = 5
}

public enum UrgencyLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum PlanItemDecision
{
    Pending = 0,
    Accepted = 1,
    Deferred = 2,
    Rejected = 3
}

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}

public enum CoverageType
{
    Statutory = 0,
    Private = 1
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public enum PaymentPlanStatus
{
    Active = 0,
    Completed = 1,
    Defaulted = 2,
    Cancelled = 3
}
