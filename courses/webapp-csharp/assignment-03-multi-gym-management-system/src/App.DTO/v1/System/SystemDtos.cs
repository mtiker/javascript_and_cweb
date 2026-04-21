using App.Domain.Enums;

namespace App.DTO.v1.System;

public class RegisterGymRequest
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? RegistrationCode { get; set; }
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = "Estonia";
    public string OwnerEmail { get; set; } = default!;
    public string OwnerPassword { get; set; } = default!;
    public string OwnerFirstName { get; set; } = default!;
    public string OwnerLastName { get; set; } = default!;
}

public class RegisterGymResponse
{
    public Guid GymId { get; set; }
    public string GymCode { get; set; } = default!;
    public Guid OwnerUserId { get; set; }
}

public class GymSummaryResponse
{
    public Guid GymId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
}

public class UpdateGymActivationRequest
{
    public bool IsActive { get; set; }
}

public class SubscriptionSummaryResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public decimal MonthlyPrice { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class UpdateSubscriptionRequest
{
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal MonthlyPrice { get; set; }
}

public class SupportTicketRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Medium;
}

public class SupportTicketResponse
{
    public Guid TicketId { get; set; }
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public string Title { get; set; } = default!;
    public SupportTicketStatus Status { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CompanySnapshotResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public int MemberCount { get; set; }
    public int SessionCount { get; set; }
    public int OpenMaintenanceTaskCount { get; set; }
}

public class PlatformAnalyticsResponse
{
    public int GymCount { get; set; }
    public int UserCount { get; set; }
    public int MemberCount { get; set; }
    public int OpenSupportTicketCount { get; set; }
}

public class StartImpersonationRequest
{
    public Guid UserId { get; set; }
    public string? GymCode { get; set; }
}

public class StartImpersonationResponse
{
    public string Jwt { get; set; } = default!;
    public Guid UserId { get; set; }
    public string? GymCode { get; set; }
}
