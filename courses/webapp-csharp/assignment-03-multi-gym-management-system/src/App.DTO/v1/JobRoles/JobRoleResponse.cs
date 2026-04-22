using App.Domain.Enums;

namespace App.DTO.v1.JobRoles;

public class JobRoleResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
}
