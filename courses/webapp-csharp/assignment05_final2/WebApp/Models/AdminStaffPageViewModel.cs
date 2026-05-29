using System.ComponentModel.DataAnnotations;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Models;

public class AdminStaffPageViewModel
{
    public string GymCode { get; set; } = default!;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int SuspendedCount { get; set; }
    public int InactiveCount { get; set; }
    public IReadOnlyCollection<AdminStaffSummaryViewModel> Staff { get; set; } = [];
}

public class AdminStaffSummaryViewModel
{
    public Guid Id { get; set; }
    public string StaffCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public StaffStatus Status { get; set; }
}

public class AdminStaffFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = default!;

    [Required]
    [StringLength(50)]
    [Display(Name = "Staff code")]
    public string StaffCode { get; set; } = default!;

    [Display(Name = "Status")]
    public StaffStatus Status { get; set; } = StaffStatus.Active;

    [BindNever]
    public string? GymCode { get; set; }

    public bool IsEdit => Id.HasValue;
}

public class AdminStaffDeleteViewModel
{
    public Guid Id { get; set; }
    public string StaffCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public StaffStatus Status { get; set; }
    public string? GymCode { get; set; }
}
