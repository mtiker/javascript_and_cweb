namespace App.DTO.v1.Appointments;

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DentistId { get; set; }
    public Guid TreatmentRoomId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string Status { get; set; } = default!;
    public string? Notes { get; set; }
}
