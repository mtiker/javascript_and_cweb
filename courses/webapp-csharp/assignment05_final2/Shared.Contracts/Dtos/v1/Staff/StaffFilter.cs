using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Staff;

public class StaffFilter
{
    public StaffStatus? Status { get; set; }
    public string? Search { get; set; }
}
