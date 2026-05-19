using App.Domain.Enums;

namespace App.DTO.v1.MaintenanceTasks;

public class MaintenanceTaskFilter
{
    public MaintenanceTaskStatus? Status { get; set; }
    public MaintenancePriority? Priority { get; set; }
    public MaintenanceTaskType? TaskType { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public DateTime? DueBeforeUtc { get; set; }
}
