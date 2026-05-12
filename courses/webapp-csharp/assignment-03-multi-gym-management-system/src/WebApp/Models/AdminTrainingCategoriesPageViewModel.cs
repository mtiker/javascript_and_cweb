using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Models;

public class AdminTrainingCategoriesPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<AdminTrainingCategorySummaryViewModel> Categories { get; set; } = [];
}

public class AdminTrainingCategorySummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public IReadOnlyCollection<string> AlternateNames { get; set; } = [];
    public string? Description { get; set; }
    public IReadOnlyCollection<string> AlternateDescriptions { get; set; } = [];
}

public class AdminTrainingCategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    [Display(Name = "Name")]
    public string Name { get; set; } = default!;

    [StringLength(512)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [BindNever]
    public string? GymCode { get; set; }

    [BindNever]
    public IReadOnlyCollection<string> AlternateNames { get; set; } = [];

    [BindNever]
    public IReadOnlyCollection<string> AlternateDescriptions { get; set; } = [];

    public bool IsEdit => Id.HasValue;
}

public class AdminTrainingCategoryDeleteViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public IReadOnlyCollection<string> AlternateNames { get; set; } = [];
    public string? Description { get; set; }
    public string? GymCode { get; set; }
}
