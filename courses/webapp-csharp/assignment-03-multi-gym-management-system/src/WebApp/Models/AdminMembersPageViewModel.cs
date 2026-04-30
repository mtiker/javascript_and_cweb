using App.Domain.Enums;

namespace WebApp.Models;

public class AdminMembersPageViewModel
{
    public string GymCode { get; set; } = default!;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int SuspendedCount { get; set; }
    public int LeftCount { get; set; }
    public IReadOnlyCollection<AdminMemberSummaryViewModel> Members { get; set; } = [];
}

public class AdminMemberSummaryViewModel
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public MemberStatus Status { get; set; }
}
