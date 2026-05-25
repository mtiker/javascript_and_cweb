using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.GymSettings;

public class GymSettingsUpdateRequest
{
    public string CurrencyCode { get; set; } = "EUR";
    public string TimeZone { get; set; } = "Europe/Tallinn";
    public bool AllowNonMemberBookings { get; set; }
    public int BookingCancellationHours { get; set; }
    public string? PublicDescription { get; set; }
}
