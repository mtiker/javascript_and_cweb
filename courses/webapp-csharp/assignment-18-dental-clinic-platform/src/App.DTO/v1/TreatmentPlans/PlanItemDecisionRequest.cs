using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentPlans;

public class PlanItemDecisionRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public Guid PlanItemId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Decision { get; set; } = default!;

    [MaxLength(512)]
    public string? Notes { get; set; }
}
