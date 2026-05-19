using App.Domain.Enums;

namespace WebApp.Models;

public class AdminMembershipsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<MembershipPackageSummaryViewModel> Packages { get; set; } = [];
    public IReadOnlyCollection<ActiveMembershipSummaryViewModel> ActiveMemberships { get; set; } = [];
}

public class MembershipPackageSummaryViewModel
{
    public string Name { get; set; } = default!;
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public bool IsTrainingFree { get; set; }
    public int? TrainingDiscountPercent { get; set; }
}

public class ActiveMembershipSummaryViewModel
{
    public string MemberName { get; set; } = default!;
    public string PackageName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MembershipStatus Status { get; set; }
}
