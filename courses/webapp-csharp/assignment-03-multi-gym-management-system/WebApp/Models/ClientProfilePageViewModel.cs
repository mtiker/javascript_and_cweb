using App.Domain.Enums;

namespace WebApp.Models;

public class ClientProfilePageViewModel
{
    public string GymCode { get; set; } = default!;
    public string? ActiveRole { get; set; }
    public bool ProfileAvailable { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public MemberStatus? MemberStatus { get; set; }
    public IReadOnlyCollection<ClientMembershipSummaryViewModel> Memberships { get; set; } = [];
    public IReadOnlyCollection<ClientBookingSummaryViewModel> Bookings { get; set; } = [];
    public IReadOnlyCollection<ClientPaymentSummaryViewModel> Payments { get; set; } = [];
}

public class ClientMembershipSummaryViewModel
{
    public string PackageName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MembershipStatus Status { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
}

public class ClientBookingSummaryViewModel
{
    public string SessionName { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public BookingStatus Status { get; set; }
    public decimal ChargedPrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
}

public class ClientPaymentSummaryViewModel
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public PaymentStatus Status { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string? Reference { get; set; }
}
