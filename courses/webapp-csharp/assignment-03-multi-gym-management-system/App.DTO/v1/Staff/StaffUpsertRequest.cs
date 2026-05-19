using App.Domain.Enums;

namespace App.DTO.v1.Staff;

public class StaffUpsertRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string StaffCode { get; set; } = default!;
    public StaffStatus Status { get; set; } = StaffStatus.Active;
}
