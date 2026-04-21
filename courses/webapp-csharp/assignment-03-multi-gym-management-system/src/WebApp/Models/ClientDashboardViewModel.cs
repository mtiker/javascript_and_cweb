using App.DTO.v1.Tenant;

namespace WebApp.Models;

public class ClientDashboardViewModel
{
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public IReadOnlyCollection<TrainingSessionResponse> UpcomingSessions { get; set; } = [];
    public IReadOnlyCollection<BookingResponse> MyBookings { get; set; } = [];
    public IReadOnlyCollection<MaintenanceTaskResponse> AssignedTasks { get; set; } = [];
}
