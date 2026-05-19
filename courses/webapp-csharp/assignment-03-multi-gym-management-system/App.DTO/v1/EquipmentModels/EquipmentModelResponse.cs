using App.Domain.Enums;

namespace App.DTO.v1.EquipmentModels;

public class EquipmentModelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public EquipmentType Type { get; set; }
    public string? Manufacturer { get; set; }
    public int MaintenanceIntervalDays { get; set; }
    public string? Description { get; set; }
}
