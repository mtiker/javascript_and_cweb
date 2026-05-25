using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Equipment;

public class EquipmentStatusUpdateRequest
{
    public EquipmentStatus Status { get; set; }
}
