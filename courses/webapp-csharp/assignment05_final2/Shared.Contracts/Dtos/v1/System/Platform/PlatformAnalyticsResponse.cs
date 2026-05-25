using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.System.Platform;

public class PlatformAnalyticsResponse
{
    public int GymCount { get; set; }
    public int UserCount { get; set; }
    public int MemberCount { get; set; }
    public int ActiveMaintenanceTaskCount { get; set; }
}
