using App.Domain.Common;

namespace App.Domain.Entities;

public class Treatment : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public Guid TreatmentTypeId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? DentistId { get; set; }
    public int? ToothNumber { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public Appointment? Appointment { get; set; }
    public Dentist? Dentist { get; set; }
}
