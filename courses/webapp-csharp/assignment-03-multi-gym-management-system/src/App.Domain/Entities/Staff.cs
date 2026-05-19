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

    public ICollection<MaintenanceTask> AssignedTasks { get; set; } = new List<MaintenanceTask>();
    public ICollection<MaintenanceTask> CreatedTasks { get; set; } = new List<MaintenanceTask>();
}
