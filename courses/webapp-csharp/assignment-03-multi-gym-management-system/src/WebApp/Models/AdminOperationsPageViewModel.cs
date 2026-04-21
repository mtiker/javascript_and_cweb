using App.Domain.Enums;

namespace WebApp.Models;

public class AdminOperationsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<OpeningHoursSummaryViewModel> OpeningHours { get; set; } = [];
    public IReadOnlyCollection<EquipmentSummaryViewModel> Equipment { get; set; } = [];
    public IReadOnlyCollection<MaintenanceSummaryViewModel> MaintenanceTasks { get; set; } = [];
}

public class OpeningHoursSummaryViewModel
{
    public int Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class EquipmentSummaryViewModel
{
    public string AssetTag { get; set; } = default!;
    public string ModelName { get; set; } = default!;
    public EquipmentStatus Status { get; set; }
}

public class MaintenanceSummaryViewModel
{
    public string AssetTag { get; set; } = default!;
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueAtUtc { get; set; }
}
