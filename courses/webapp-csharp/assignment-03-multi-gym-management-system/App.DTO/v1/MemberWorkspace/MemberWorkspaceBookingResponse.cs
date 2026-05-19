using App.Domain.Enums;

namespace App.DTO.v1.MemberWorkspace;

public class MemberWorkspaceBookingResponse
{
    public Guid BookingId { get; set; }
    public Guid TrainingSessionId { get; set; }
    public string TrainingSessionName { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public BookingStatus Status { get; set; }
    public decimal ChargedPrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public bool PaymentRequired { get; set; }
}
