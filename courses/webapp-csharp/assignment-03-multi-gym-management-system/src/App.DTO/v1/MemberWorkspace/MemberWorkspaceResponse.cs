using App.DTO.v1.Finance;
using App.DTO.v1.Members;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.DTO.v1.MemberWorkspace;

public class MemberWorkspaceResponse
{
    public MemberDetailResponse Profile { get; set; } = default!;
    public IReadOnlyCollection<MembershipResponse> Memberships { get; set; } = [];
    public IReadOnlyCollection<PaymentResponse> Payments { get; set; } = [];
    public IReadOnlyCollection<MemberWorkspaceBookingResponse> Bookings { get; set; } = [];
    public IReadOnlyCollection<InvoiceResponse> Invoices { get; set; } = [];
    public int AttendedSessionCount { get; set; }
    public int UpcomingBookingCount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public IReadOnlyCollection<MemberOutstandingActionResponse> OutstandingActions { get; set; } = [];
}
