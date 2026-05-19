using App.DTO.v1.TrainingSessions;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.Bookings;

namespace WebApp.Models;

public class ClientDashboardViewModel
{
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public IReadOnlyCollection<TrainingSessionResponse> UpcomingSessions { get; set; } = [];
    public IReadOnlyCollection<BookingResponse> MyBookings { get; set; } = [];
    public IReadOnlyCollection<MaintenanceTaskResponse> AssignedTasks { get; set; } = [];
}
