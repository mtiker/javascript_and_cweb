using App.Domain.Enums;

namespace App.DTO.v1.WorkShifts;

public class WorkShiftResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftType ShiftType { get; set; }
    public Guid? TrainingSessionId { get; set; }
    public string? Comment { get; set; }
}
