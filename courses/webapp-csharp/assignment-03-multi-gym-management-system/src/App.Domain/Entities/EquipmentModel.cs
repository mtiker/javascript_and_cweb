using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class EquipmentModel : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Equipment", "en");

    public EquipmentType Type { get; set; }

    [MaxLength(64)]
    public string? Manufacturer { get; set; }

    public int MaintenanceIntervalDays { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<Equipment> EquipmentItems { get; set; } = new List<Equipment>();
}
