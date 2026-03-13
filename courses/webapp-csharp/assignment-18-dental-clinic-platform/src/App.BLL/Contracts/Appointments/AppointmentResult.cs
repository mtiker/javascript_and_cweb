namespace App.BLL.Contracts.Appointments;

public sealed record AppointmentResult(
    Guid Id,
    Guid PatientId,
    Guid DentistId,
    Guid TreatmentRoomId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    string? Notes);
