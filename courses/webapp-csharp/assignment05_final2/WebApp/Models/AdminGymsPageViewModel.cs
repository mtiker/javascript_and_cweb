namespace WebApp.Models;

public class AdminGymsPageViewModel
{
    public IReadOnlyCollection<AdminGymSummaryViewModel> Gyms { get; set; } = [];
}

public class AdminGymSummaryViewModel
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string City { get; set; } = default!;
    public bool IsActive { get; set; }
}
