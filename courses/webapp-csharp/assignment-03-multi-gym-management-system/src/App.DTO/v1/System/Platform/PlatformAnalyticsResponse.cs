using App.Domain.Enums;

namespace App.DTO.v1.System.Platform;

public class PlatformAnalyticsResponse
{
    public int GymCount { get; set; }
    public int UserCount { get; set; }
    public int MemberCount { get; set; }
    public int OpenSupportTicketCount { get; set; }
}
