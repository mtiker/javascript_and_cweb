using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DentistId { get; set; }

    [Required]
    public Guid TreatmentRoomId { get; set; }

    [Required]
    public DateTime StartAtUtc { get; set; }

    [Required]
    public DateTime EndAtUtc { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
