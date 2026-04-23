using System.ComponentModel.DataAnnotations;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class CoachingPlanItem : TenantBaseEntity
{
    public Guid CoachingPlanId { get; set; }
    public CoachingPlan? CoachingPlan { get; set; }

    public int Sequence { get; set; }

    [MaxLength(256)]
    public string Title { get; set; } = default!;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateOnly? TargetDate { get; set; }
    public CoachingPlanItemDecision? Decision { get; set; }
    public DateTime? DecisionAtUtc { get; set; }

    public Guid? DecisionByStaffId { get; set; }
    public Staff? DecisionByStaff { get; set; }

    [MaxLength(1000)]
    public string? DecisionNotes { get; set; }
}
