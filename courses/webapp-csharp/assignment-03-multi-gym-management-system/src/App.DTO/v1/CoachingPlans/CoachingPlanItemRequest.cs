using App.Domain.Enums;

namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanItemRequest
{
    public int Sequence { get; set; }
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
}
