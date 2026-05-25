using System.ComponentModel.DataAnnotations;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Models;

public class AdminMembershipPackagesPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<AdminMembershipPackageSummaryViewModel> Packages { get; set; } = [];
}

public class AdminMembershipPackageSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public bool IsTrainingFree { get; set; }
    public int? TrainingDiscountPercent { get; set; }
}

public class AdminMembershipPackageFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 1)]
    [Display(Name = "Name")]
    public string Name { get; set; } = default!;

    [Display(Name = "Package type")]
    public MembershipPackageType PackageType { get; set; } = MembershipPackageType.Monthly;

    [Range(1, 3650)]
    [Display(Name = "Duration value")]
    public int DurationValue { get; set; } = 1;

    [Display(Name = "Duration unit")]
    public DurationUnit DurationUnit { get; set; } = DurationUnit.Month;

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Base price")]
    public decimal BasePrice { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency code must be a three-letter ISO code.")]
    [Display(Name = "Currency code")]
    public string CurrencyCode { get; set; } = "EUR";

    [Range(0, 100)]
    [Display(Name = "Training discount %")]
    public int? TrainingDiscountPercent { get; set; }

    [Display(Name = "Training is free")]
    public bool IsTrainingFree { get; set; }

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [BindNever]
    public string? GymCode { get; set; }

    public bool IsEdit => Id.HasValue;
}

public class AdminMembershipPackageDeleteViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? GymCode { get; set; }
}
