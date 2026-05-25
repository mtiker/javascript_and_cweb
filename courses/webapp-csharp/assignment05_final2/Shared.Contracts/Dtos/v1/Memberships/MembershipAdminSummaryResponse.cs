using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Memberships;

public class MembershipAdminSummaryResponse
{
    public Guid Id { get; set; }
    public string MemberName { get; set; } = default!;
    public string PackageName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MembershipStatus Status { get; set; }
}
