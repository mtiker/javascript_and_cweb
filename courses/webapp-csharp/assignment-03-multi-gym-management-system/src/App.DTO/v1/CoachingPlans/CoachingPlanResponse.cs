using App.Domain.Enums;

namespace App.DTO.v1.CoachingPlans;

public class CoachingPlanResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public Guid? TrainerStaffId { get; set; }
    public string? TrainerStaffName { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public CoachingPlanStatus Status { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public IReadOnlyCollection<CoachingPlanItemResponse> Items { get; set; } = [];
}
