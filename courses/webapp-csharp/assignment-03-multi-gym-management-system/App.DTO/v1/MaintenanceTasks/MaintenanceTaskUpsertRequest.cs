using App.Domain.Enums;

namespace App.DTO.v1.MaintenanceTasks;

public class MaintenanceTaskUpsertRequest
{
    public Guid EquipmentId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Open;
    public DateTime? DueAtUtc { get; set; }
    public string? Notes { get; set; }
}
