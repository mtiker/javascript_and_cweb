using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class ToothRecord : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public int ToothNumber { get; set; }
    public ToothConditionStatus Condition { get; set; }
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
}
