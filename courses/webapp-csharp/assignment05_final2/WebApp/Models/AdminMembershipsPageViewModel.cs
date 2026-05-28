using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Contracts.Enums;

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
    public Guid Id { get; set; }
    public string MemberName { get; set; } = default!;
    public string PackageName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MembershipStatus Status { get; set; }
}

public class AdminMembershipSellFormViewModel
{
    public string? GymCode { get; set; }

    [Required]
    [Display(Name = "Member")]
    public Guid MemberId { get; set; }

    [Required]
    [Display(Name = "Package")]
    public Guid MembershipPackageId { get; set; }

    [Display(Name = "StartDate")]
    public DateOnly? RequestedStartDate { get; set; }

    [StringLength(100)]
    [Display(Name = "PaymentReference")]
    public string? PaymentReference { get; set; }

    public IReadOnlyCollection<SelectListItem> MemberOptions { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> PackageOptions { get; set; } = [];
}

public class AdminMembershipEditFormViewModel
{
    public Guid Id { get; set; }
    public string? GymCode { get; set; }
    public string MemberName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Package")]
    public Guid MembershipPackageId { get; set; }

    [Display(Name = "StartDate")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "EndDate")]
    public DateOnly EndDate { get; set; }

    public MembershipStatus Status { get; set; }

    public IReadOnlyCollection<SelectListItem> PackageOptions { get; set; } = [];
}

public class AdminMembershipDeleteViewModel
{
    public Guid Id { get; set; }
    public string GymCode { get; set; } = default!;
    public string MemberName { get; set; } = default!;
    public string PackageName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MembershipStatus Status { get; set; }
}
