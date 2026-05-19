using System.ComponentModel.DataAnnotations;
using App.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

public class AdminMemberFormViewModel
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
    [Display(Name = "Member code")]
    public string MemberCode { get; set; } = default!;

    [Required]
    [StringLength(50)]
    [Display(Name = "Personal code")]
    public string PersonalCode { get; set; } = default!;

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [BindNever]
    public string? GymCode { get; set; }

    public bool IsEdit => Id.HasValue;
}

public class AdminMemberDeleteViewModel
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public MemberStatus Status { get; set; }
    public string? GymCode { get; set; }
}
