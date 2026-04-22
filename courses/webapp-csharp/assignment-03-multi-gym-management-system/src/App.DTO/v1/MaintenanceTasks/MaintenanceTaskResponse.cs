using App.Domain.Enums;

namespace App.DTO.v1.MaintenanceTasks;

public class MaintenanceTaskResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string? EquipmentAssetTag { get; set; }
    public string EquipmentName { get; set; } = default!;
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}
