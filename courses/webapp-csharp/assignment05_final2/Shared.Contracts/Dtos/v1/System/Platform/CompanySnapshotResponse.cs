namespace Shared.Contracts.Dtos.v1.System.Platform;

public class CompanySnapshotResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public int MemberCount { get; set; }
    public int SessionCount { get; set; }
    public int OpenMaintenanceTaskCount { get; set; }
}
