using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Common;
using Base.Domain;
using Shared.Contracts.Enums;

namespace App.Domain.Entities;

public class Equipment : TenantBaseEntity
{
    public Guid EquipmentModelId { get; set; }
    public EquipmentModel? EquipmentModel { get; set; }

    [MaxLength(32)]
    public string? AssetTag { get; set; }

    [MaxLength(64)]
    public string? SerialNumber { get; set; }

    public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Active;
    public DateOnly? CommissionedAt { get; set; }
    public DateOnly? DecommissionedAt { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();
}
