namespace App.BLL.Contracts.Patients;

public sealed record PatientToothResult(
    Guid Id,
    int ToothNumber,
    string Condition,
    string? Notes,
    DateTime StatusUpdatedAtUtc,
    DateTime? LastTreatmentAtUtc,
    string? LastTreatmentTypeName,
    string? LastTreatmentNotes,
    IReadOnlyCollection<PatientToothHistoryItemResult> History);
