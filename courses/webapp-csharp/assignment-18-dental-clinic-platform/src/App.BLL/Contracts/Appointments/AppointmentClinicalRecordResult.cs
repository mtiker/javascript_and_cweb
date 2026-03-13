namespace App.BLL.Contracts.Appointments;

public sealed record AppointmentClinicalRecordResult(
    Guid AppointmentId,
    string Status,
    int RecordedItemCount);
