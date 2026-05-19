using App.Domain.Enums;

namespace App.DTO.v1.Equipment;

public class EquipmentFilter
{
    public EquipmentStatus? Status { get; set; }
    public Guid? EquipmentModelId { get; set; }
    public string? Search { get; set; }
}
