using App.Domain.Enums;

namespace App.DTO.v1.System.Support;

public class CompanySnapshotResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public int MemberCount { get; set; }
    public int SessionCount { get; set; }
    public int OpenMaintenanceTaskCount { get; set; }
}
