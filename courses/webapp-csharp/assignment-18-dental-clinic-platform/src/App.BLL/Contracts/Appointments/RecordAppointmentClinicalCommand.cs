using App.Domain.Enums;

namespace App.BLL.Contracts.Appointments;

public sealed record RecordAppointmentClinicalCommand(
    Guid AppointmentId,
    DateTime PerformedAtUtc,
    bool MarkAppointmentCompleted,
    IReadOnlyCollection<RecordAppointmentClinicalItemCommand> Items);

public sealed record RecordAppointmentClinicalItemCommand(
    Guid TreatmentTypeId,
    int ToothNumber,
    ToothConditionStatus Condition,
    decimal? Price,
    string? Notes);
