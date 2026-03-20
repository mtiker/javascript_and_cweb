using App.Domain.Entities;
using App.Domain.Enums;

namespace App.BLL.Services;

public static class TreatmentPlanWorkflow
{
    public static TreatmentPlanStatus ResolveStatus(TreatmentPlan plan)
    {
        return ResolveStatus(plan.SubmittedAtUtc, plan.Items.Select(entity => entity.Decision));
    }

    public static TreatmentPlanStatus ResolveStatus(DateTime? submittedAtUtc, IEnumerable<PlanItemDecision> decisions)
    {
        var decisionList = decisions.ToArray();
        if (decisionList.Length == 0 || submittedAtUtc == null)
        {
            return TreatmentPlanStatus.Draft;
        }

        if (decisionList.All(decision => decision == PlanItemDecision.Accepted))
        {
            return TreatmentPlanStatus.Accepted;
        }

        if (decisionList.Any(decision => decision == PlanItemDecision.Accepted) &&
            decisionList.Any(decision => decision is PlanItemDecision.Deferred or PlanItemDecision.Rejected or PlanItemDecision.Pending))
        {
            return TreatmentPlanStatus.PartiallyAccepted;
        }

        if (decisionList.All(decision => decision == PlanItemDecision.Deferred || decision == PlanItemDecision.Rejected))
        {
            return TreatmentPlanStatus.Deferred;
        }

        return TreatmentPlanStatus.Pending;
    }

    public static void ApplyDerivedState(TreatmentPlan plan, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        plan.Status = ResolveStatus(plan);
        plan.ApprovedAtUtc = plan.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.PartiallyAccepted
            ? plan.ApprovedAtUtc ?? now
            : null;
    }

    public static bool IsLockedForItemReplacement(TreatmentPlan plan)
    {
        return plan.SubmittedAtUtc.HasValue || plan.Items.Any(HasDecisionHistory);
    }

    public static bool HasDecisionHistory(PlanItem item)
    {
        return item.Decision != PlanItemDecision.Pending ||
               item.DecisionAtUtc.HasValue ||
               !string.IsNullOrWhiteSpace(item.DecisionNotes);
    }
}
