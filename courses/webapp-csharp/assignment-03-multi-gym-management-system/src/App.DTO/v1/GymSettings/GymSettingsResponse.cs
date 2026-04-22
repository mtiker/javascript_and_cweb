using App.Domain.Enums;

namespace App.DTO.v1.GymSettings;

public class GymSettingsResponse
{
    public Guid GymId { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
    public bool AllowNonMemberBookings { get; set; }
    public int BookingCancellationHours { get; set; }
    public string? PublicDescription { get; set; }
}
