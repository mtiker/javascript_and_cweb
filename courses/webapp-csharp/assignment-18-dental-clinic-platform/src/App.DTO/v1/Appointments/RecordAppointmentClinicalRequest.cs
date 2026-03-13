using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Appointments;

public class RecordAppointmentClinicalRequest
{
    [Required]
    public DateTime PerformedAtUtc { get; set; }

    public bool MarkAppointmentCompleted { get; set; }

    [Required]
    [MinLength(1)]
    public List<RecordAppointmentClinicalItemRequest> Items { get; set; } = [];
}

public class RecordAppointmentClinicalItemRequest
{
    [Required]
    public Guid TreatmentTypeId { get; set; }

    [Range(11, 48)]
    public int ToothNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string Condition { get; set; } = default!;

    [Range(0, 1000000)]
    public decimal? Price { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
