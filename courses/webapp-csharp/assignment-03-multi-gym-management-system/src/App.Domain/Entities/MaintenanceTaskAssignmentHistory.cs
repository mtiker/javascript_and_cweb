using System.ComponentModel.DataAnnotations;
using App.Domain.Common;

namespace App.Domain.Entities;

public class MaintenanceTaskAssignmentHistory : TenantBaseEntity
{
    public Guid MaintenanceTaskId { get; set; }
    public MaintenanceTask? MaintenanceTask { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public Staff? AssignedStaff { get; set; }

    public Guid? AssignedByStaffId { get; set; }
    public Staff? AssignedByStaff { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(512)]
    public string? Notes { get; set; }
}
