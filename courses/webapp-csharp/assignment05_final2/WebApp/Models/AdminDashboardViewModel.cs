namespace WebApp.Models;

public class AdminDashboardViewModel
{
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public int GymCount { get; set; }
    public int MemberCount { get; set; }
    public int SessionCount { get; set; }
    public int OpenMaintenanceTaskCount { get; set; }
    public IReadOnlyCollection<string> SystemRoles { get; set; } = [];
}
