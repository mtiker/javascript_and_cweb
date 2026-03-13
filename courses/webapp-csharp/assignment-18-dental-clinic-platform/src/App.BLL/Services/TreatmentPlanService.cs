using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class TreatmentPlanService(AppDbContext dbContext, ITenantAccessService tenantAccessService) : ITreatmentPlanService
{
    public async Task<PlanDecisionResult> RecordPlanItemDecisionAsync(
        Guid userId,
        RecordPlanItemDecisionCommand command,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager);

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == command.PlanId, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        var item = plan.Items.SingleOrDefault(entity => entity.Id == command.PlanItemId);
        if (item == null)
        {
            throw new NotFoundException("Plan item was not found.");
        }

        item.Decision = command.Decision;
        item.DecisionAtUtc = DateTime.UtcNow;
        item.DecisionNotes = command.Notes;

        plan.Status = ResolvePlanStatus(plan.Items.Select(entity => entity.Decision).ToArray());
        if (plan.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.PartiallyAccepted)
        {
            plan.ApprovedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PlanDecisionResult(plan.Id, item.Id, plan.Status.ToString(), item.Decision.ToString());
    }

    private static TreatmentPlanStatus ResolvePlanStatus(PlanItemDecision[] decisions)
    {
        if (decisions.Length == 0)
        {
            return TreatmentPlanStatus.Draft;
        }

        if (decisions.All(decision => decision == PlanItemDecision.Accepted))
        {
            return TreatmentPlanStatus.Accepted;
        }

        if (decisions.Any(decision => decision == PlanItemDecision.Accepted) &&
            decisions.Any(decision => decision is PlanItemDecision.Deferred or PlanItemDecision.Rejected or PlanItemDecision.Pending))
        {
            return TreatmentPlanStatus.PartiallyAccepted;
        }

        if (decisions.All(decision => decision == PlanItemDecision.Deferred || decision == PlanItemDecision.Rejected))
        {
            return TreatmentPlanStatus.Deferred;
        }

        return TreatmentPlanStatus.Pending;
    }
}
