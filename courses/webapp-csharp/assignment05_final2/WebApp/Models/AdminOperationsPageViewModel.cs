using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Contracts.Enums;

namespace WebApp.Models;

public class AdminOperationsPageViewModel
{
    public string GymCode { get; set; } = default!;
    public IReadOnlyCollection<EquipmentSummaryViewModel> Equipment { get; set; } = [];
    public IReadOnlyCollection<MaintenanceSummaryViewModel> MaintenanceTasks { get; set; } = [];
}

public class EquipmentSummaryViewModel
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = default!;
    public string ModelName { get; set; } = default!;
    public EquipmentStatus Status { get; set; }
}

public class MaintenanceSummaryViewModel
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = default!;
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public class AdminEquipmentFormViewModel
{
    public Guid? Id { get; set; }
    public string? GymCode { get; set; }

    [Required]
    [Display(Name = "EquipmentModel")]
    public Guid EquipmentModelId { get; set; }

    [StringLength(64)]
    [Display(Name = "AssetTag")]
    public string? AssetTag { get; set; }

    [StringLength(128)]
    [Display(Name = "SerialNumber")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Status")]
    public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Active;

    [Display(Name = "CommissionedAt")]
    public DateOnly? CommissionedAt { get; set; }

    [Display(Name = "DecommissionedAt")]
    public DateOnly? DecommissionedAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IReadOnlyCollection<SelectListItem> EquipmentModelOptions { get; set; } = [];
}

public class AdminEquipmentDeleteViewModel
{
    public Guid Id { get; set; }
    public string GymCode { get; set; } = default!;
    public string AssetTag { get; set; } = default!;
    public string ModelName { get; set; } = default!;
    public EquipmentStatus Status { get; set; }
}

public class AdminMaintenanceTaskFormViewModel
{
    public string? GymCode { get; set; }

    [Required]
    [Display(Name = "Equipment")]
    public Guid EquipmentId { get; set; }

    [Display(Name = "AssignedTo")]
    public Guid? AssignedStaffId { get; set; }

    [Display(Name = "Type")]
    public MaintenanceTaskType TaskType { get; set; } = MaintenanceTaskType.Scheduled;

    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Open;

    [Display(Name = "Due")]
    public DateTime? DueAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IReadOnlyCollection<SelectListItem> EquipmentOptions { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> StaffOptions { get; set; } = [];
}

public class AdminMaintenanceTaskEditFormViewModel
{
    public Guid Id { get; set; }
    public string? GymCode { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; }

    public MaintenanceTaskStatus Status { get; set; }

    [Display(Name = "AssignedTo")]
    public Guid? AssignedStaffId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IReadOnlyCollection<SelectListItem> StaffOptions { get; set; } = [];
}

public class AdminMaintenanceTaskDeleteViewModel
{
    public Guid Id { get; set; }
    public string GymCode { get; set; } = default!;
    public string EquipmentName { get; set; } = default!;
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
}
