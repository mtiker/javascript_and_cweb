using App.Domain.Enums;

namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanItemResponse
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
    public CoachingPlanItemDecision? Decision { get; set; }
    public DateTime? DecisionAtUtc { get; set; }
    public string? DecisionByStaffName { get; set; }
    public string? DecisionNotes { get; set; }
}
