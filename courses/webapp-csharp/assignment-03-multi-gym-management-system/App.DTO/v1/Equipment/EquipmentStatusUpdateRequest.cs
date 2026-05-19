using App.Domain.Enums;

namespace App.DTO.v1.Equipment;

public class EquipmentStatusUpdateRequest
{
    public EquipmentStatus Status { get; set; }
}
