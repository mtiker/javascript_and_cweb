using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Appointment : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DentistId { get; set; }
    public Guid TreatmentRoomId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
    public Dentist? Dentist { get; set; }
    public TreatmentRoom? TreatmentRoom { get; set; }
    public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}
