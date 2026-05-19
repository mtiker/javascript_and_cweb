namespace WebApp.Models;

public class WorkspaceSwitcherViewModel
{
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public string ReturnUrl { get; set; } = "/workspace";
    public IReadOnlyCollection<WorkspaceGymOptionViewModel> Gyms { get; set; } = [];
    public IReadOnlyCollection<string> RolesInActiveGym { get; set; } = [];
}

public class WorkspaceGymOptionViewModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
