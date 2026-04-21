using App.DTO.v1.Tenant;
using App.Domain.Enums;

namespace WebApp.Models;

public class MaintenancePageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<MaintenanceTaskResponse> Tasks { get; set; } = [];
}

public class MaintenanceTaskDetailPageViewModel
{
    public string GymCode { get; set; } = default!;
    public MaintenanceTaskResponse Task { get; set; } = default!;
    public string EquipmentLabel { get; set; } = default!;
    public MaintenanceTaskStatus NextStatus { get; set; }
    public string? Notes { get; set; }
}
