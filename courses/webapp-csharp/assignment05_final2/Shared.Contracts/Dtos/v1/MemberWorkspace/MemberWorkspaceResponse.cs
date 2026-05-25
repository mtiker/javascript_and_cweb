using Shared.Contracts.Dtos.v1.Members;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Payments;

namespace Shared.Contracts.Dtos.v1.MemberWorkspace;

public class MemberWorkspaceResponse
{
    public MemberDetailResponse Profile { get; set; } = default!;
    public IReadOnlyCollection<MembershipResponse> Memberships { get; set; } = [];
    public IReadOnlyCollection<PaymentResponse> Payments { get; set; } = [];
    public IReadOnlyCollection<MemberWorkspaceBookingResponse> Bookings { get; set; } = [];
    public int AttendedSessionCount { get; set; }
    public int UpcomingBookingCount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public IReadOnlyCollection<MemberOutstandingActionResponse> OutstandingActions { get; set; } = [];
}
