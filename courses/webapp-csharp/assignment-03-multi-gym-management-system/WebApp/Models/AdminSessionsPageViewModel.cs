using App.Domain.Enums;

namespace WebApp.Models;

public class AdminSessionsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<AdminSessionSummaryViewModel> Sessions { get; set; } = [];
}

public class AdminSessionSummaryViewModel
{
    public string Name { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int Capacity { get; set; }
    public int BookingCount { get; set; }
    public TrainingSessionStatus Status { get; set; }
    public string TrainerNames { get; set; } = default!;
}
