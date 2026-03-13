namespace App.BLL.Contracts.Patients;

public sealed record PatientToothHistoryItemResult(
    Guid Id,
    Guid? AppointmentId,
    string TreatmentTypeName,
    DateTime PerformedAtUtc,
    decimal Price,
    string? Notes);
