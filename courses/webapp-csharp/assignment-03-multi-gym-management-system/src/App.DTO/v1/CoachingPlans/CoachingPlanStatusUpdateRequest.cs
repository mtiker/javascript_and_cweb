using App.Domain.Enums;

namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanStatusUpdateRequest
{
    public CoachingPlanStatus Status { get; set; }
    public string? Notes { get; set; }
}
