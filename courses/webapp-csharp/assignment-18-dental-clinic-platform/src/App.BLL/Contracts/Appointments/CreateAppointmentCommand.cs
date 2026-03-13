namespace App.BLL.Contracts.Appointments;

public sealed record CreateAppointmentCommand(
    Guid PatientId,
    Guid DentistId,
    Guid TreatmentRoomId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Notes);
