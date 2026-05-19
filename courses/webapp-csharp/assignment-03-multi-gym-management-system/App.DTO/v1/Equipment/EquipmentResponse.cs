using App.Domain.Enums;

namespace App.DTO.v1.Equipment;

public class EquipmentResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentModelId { get; set; }
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public EquipmentStatus CurrentStatus { get; set; }
    public DateOnly? CommissionedAt { get; set; }
    public DateOnly? DecommissionedAt { get; set; }
    public string? Notes { get; set; }
}
