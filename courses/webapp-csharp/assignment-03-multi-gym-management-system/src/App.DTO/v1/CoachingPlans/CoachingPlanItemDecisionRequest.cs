using App.Domain.Enums;

namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanItemDecisionRequest
{
    public CoachingPlanItemDecision Decision { get; set; }
    public string? Notes { get; set; }
}
