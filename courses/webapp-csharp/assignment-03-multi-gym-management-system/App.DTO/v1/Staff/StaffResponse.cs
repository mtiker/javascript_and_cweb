using App.Domain.Enums;

namespace App.DTO.v1.Staff;

public class StaffResponse
{
    public Guid Id { get; set; }
    public string StaffCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public StaffStatus Status { get; set; }
}
