using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Contracts.Enums;

namespace WebApp.Models;

public class AdminSessionsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<AdminSessionSummaryViewModel> Sessions { get; set; } = [];
}

public class AdminSessionSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int Capacity { get; set; }
    public int BookingCount { get; set; }
    public TrainingSessionStatus Status { get; set; }
    public string TrainerNames { get; set; } = default!;
}

public class AdminSessionFormViewModel
{
    public Guid? Id { get; set; }
    public string? GymCode { get; set; }

    [Required]
    [Display(Name = "Category")]
    public Guid CategoryId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = default!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "StartTime")]
    public DateTime StartAt { get; set; }

    [Required]
    [Display(Name = "EndTime")]
    public DateTime EndAt { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; } = 1;

    [Range(0, 100000)]
    public decimal BasePrice { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = "EUR";

    public TrainingSessionStatus Status { get; set; } = TrainingSessionStatus.Draft;

    [Display(Name = "Trainer")]
    public Guid? TrainerStaffId { get; set; }

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> TrainerOptions { get; set; } = [];
}

public class AdminSessionDeleteViewModel
{
    public Guid Id { get; set; }
    public string GymCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int BookingCount { get; set; }
    public TrainingSessionStatus Status { get; set; }
}
