using App.DTO.v1.Tenant;
using App.Domain.Enums;

namespace WebApp.Models;

public class SessionsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<TrainingSessionResponse> Sessions { get; set; } = [];
    public IReadOnlyCollection<OpeningHoursResponse> OpeningHours { get; set; } = [];
    public IReadOnlyCollection<OpeningHoursExceptionResponse> OpeningHourExceptions { get; set; } = [];
}

public class SessionDetailPageViewModel
{
    public string GymCode { get; set; } = default!;
    public TrainingSessionResponse Session { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public IReadOnlyCollection<string> TrainerNames { get; set; } = [];
    public IReadOnlyCollection<OpeningHoursResponse> OpeningHours { get; set; } = [];
    public IReadOnlyCollection<OpeningHoursExceptionResponse> OpeningHourExceptions { get; set; } = [];
    public Guid? CurrentMemberId { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public BookingStatus? CurrentBookingStatus { get; set; }
    public bool CanManageRoster { get; set; }
    public bool CanBook => CurrentMemberId.HasValue && CurrentBookingId == null && Session.Status == TrainingSessionStatus.Published;
}

public class TrainerRosterPageViewModel
{
    public string GymCode { get; set; } = default!;
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public IReadOnlyCollection<TrainerRosterBookingViewModel> Bookings { get; set; } = [];
}

public class TrainerRosterBookingViewModel
{
    public Guid BookingId { get; set; }
    public string MemberName { get; set; } = default!;
    public BookingStatus Status { get; set; }
    public decimal ChargedPrice { get; set; }
    public bool PaymentRequired { get; set; }
}
