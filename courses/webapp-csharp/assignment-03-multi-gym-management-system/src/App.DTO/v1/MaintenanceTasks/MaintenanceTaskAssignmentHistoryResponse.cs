namespace App.DTO.v1.MaintenanceTasks;

public class MaintenanceTaskAssignmentHistoryResponse
{
    public Guid Id { get; set; }
    public Guid MaintenanceTaskId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public Guid? AssignedByStaffId { get; set; }
    public string? AssignedByStaffName { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public string? Notes { get; set; }
}
