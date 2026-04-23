namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanCreateRequest
{
    public Guid MemberId { get; set; }
    public Guid? TrainerStaffId { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public IReadOnlyCollection<CoachingPlanItemRequest> Items { get; set; } = [];
}
