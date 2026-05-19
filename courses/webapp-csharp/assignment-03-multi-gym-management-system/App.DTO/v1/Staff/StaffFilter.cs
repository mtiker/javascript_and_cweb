using App.Domain.Enums;

namespace App.DTO.v1.Staff;

public class StaffFilter
{
    public StaffStatus? Status { get; set; }
    public string? Search { get; set; }
}
