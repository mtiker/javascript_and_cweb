using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.ToothRecords;

public class UpsertToothRecordRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Range(1, 32)]
    public int ToothNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string Condition { get; set; } = default!;

    [MaxLength(512)]
    public string? Notes { get; set; }
}
