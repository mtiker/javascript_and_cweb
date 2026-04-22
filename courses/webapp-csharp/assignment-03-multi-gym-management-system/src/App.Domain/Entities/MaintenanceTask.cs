using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class MaintenanceTask : TenantBaseEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public Staff? AssignedStaff { get; set; }

    public Guid? CreatedByStaffId { get; set; }
    public Staff? CreatedByStaff { get; set; }

    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Open;
    public DateTime? DueAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
