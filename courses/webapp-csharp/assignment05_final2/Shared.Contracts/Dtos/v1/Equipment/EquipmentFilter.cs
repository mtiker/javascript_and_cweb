using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Equipment;

public class EquipmentFilter
{
    public EquipmentStatus? Status { get; set; }
    public Guid? EquipmentModelId { get; set; }
    public string? Search { get; set; }
}
