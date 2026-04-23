using System.ComponentModel.DataAnnotations;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class CoachingPlan : TenantBaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? TrainerStaffId { get; set; }
    public Staff? TrainerStaff { get; set; }

    public Guid? CreatedByStaffId { get; set; }
    public Staff? CreatedByStaff { get; set; }

    [MaxLength(256)]
    public string Title { get; set; } = default!;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public CoachingPlanStatus Status { get; set; } = CoachingPlanStatus.Draft;
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<CoachingPlanItem> Items { get; set; } = new List<CoachingPlanItem>();
}
