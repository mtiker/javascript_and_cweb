namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanUpdateRequest
{
    public Guid? TrainerStaffId { get; set; }
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public IReadOnlyCollection<CoachingPlanItemRequest> Items { get; set; } = [];
}
