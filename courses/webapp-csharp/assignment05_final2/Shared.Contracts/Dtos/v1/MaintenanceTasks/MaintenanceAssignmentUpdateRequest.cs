namespace Shared.Contracts.Dtos.v1.MaintenanceTasks;

public class MaintenanceAssignmentUpdateRequest
{
    public Guid? AssignedStaffId { get; set; }
    public Guid? AssignedByStaffId { get; set; }
    public string? Notes { get; set; }
}
