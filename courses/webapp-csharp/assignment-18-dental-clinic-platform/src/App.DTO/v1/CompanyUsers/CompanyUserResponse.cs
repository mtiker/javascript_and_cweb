namespace App.DTO.v1.CompanyUsers;

public class CompanyUserResponse
{
    public Guid AppUserId { get; set; }
    public string Email { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime AssignedAtUtc { get; set; }
}
