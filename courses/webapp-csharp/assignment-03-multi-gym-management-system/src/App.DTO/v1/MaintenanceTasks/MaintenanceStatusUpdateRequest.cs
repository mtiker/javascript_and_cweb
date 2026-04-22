using App.Domain.Enums;

namespace App.DTO.v1.MaintenanceTasks;

public class MaintenanceStatusUpdateRequest
{
    public MaintenanceTaskStatus Status { get; set; }
    public string? Notes { get; set; }
}
