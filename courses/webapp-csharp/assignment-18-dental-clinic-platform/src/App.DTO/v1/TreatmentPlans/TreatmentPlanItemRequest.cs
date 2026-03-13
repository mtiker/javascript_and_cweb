using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentPlans;

public class TreatmentPlanItemRequest
{
    [Required]
    public Guid TreatmentTypeId { get; set; }

    [Range(1, 500)]
    public int Sequence { get; set; }

    [Required]
    [MaxLength(32)]
    public string Urgency { get; set; } = default!;

    [Range(0, 999999999)]
    public decimal EstimatedPrice { get; set; }
}
