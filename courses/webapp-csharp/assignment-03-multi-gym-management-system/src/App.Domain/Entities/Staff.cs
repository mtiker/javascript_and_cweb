using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Staff : TenantBaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    [MaxLength(32)]
    public string StaffCode { get; set; } = default!;

    public StaffStatus Status { get; set; } = StaffStatus.Active;
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<EmploymentContract> Contracts { get; set; } = new List<EmploymentContract>();
    public ICollection<MaintenanceTask> AssignedTasks { get; set; } = new List<MaintenanceTask>();
    public ICollection<MaintenanceTask> CreatedTasks { get; set; } = new List<MaintenanceTask>();
    public ICollection<CoachingPlan> CoachingPlans { get; set; } = new List<CoachingPlan>();
    public ICollection<CoachingPlanItem> CoachingPlanItemDecisions { get; set; } = new List<CoachingPlanItem>();
    public ICollection<MaintenanceTaskAssignmentHistory> MaintenanceAssignmentEvents { get; set; } = new List<MaintenanceTaskAssignmentHistory>();
    public ICollection<MaintenanceTaskAssignmentHistory> MaintenanceAssignmentChanges { get; set; } = new List<MaintenanceTaskAssignmentHistory>();
}
