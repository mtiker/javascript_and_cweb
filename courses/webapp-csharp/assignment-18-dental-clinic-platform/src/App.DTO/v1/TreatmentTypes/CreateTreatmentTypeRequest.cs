using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentTypes;

public class CreateTreatmentTypeRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [Range(1, 600)]
    public int DefaultDurationMinutes { get; set; }

    [Range(0, 999999999)]
    public decimal BasePrice { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }
}
