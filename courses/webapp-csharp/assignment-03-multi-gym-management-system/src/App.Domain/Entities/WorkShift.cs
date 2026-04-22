using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class WorkShift : TenantBaseEntity
{
    public Guid ContractId { get; set; }
    public EmploymentContract? Contract { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftType ShiftType { get; set; }

    public Guid? TrainingSessionId { get; set; }
    public TrainingSession? TrainingSession { get; set; }

    [MaxLength(256)]
    public string? Comment { get; set; }
}
