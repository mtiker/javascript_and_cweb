using App.BLL.Contracts;

namespace App.BLL.Services;

public interface ITreatmentPlanService
{
    Task<PlanDecisionResult> RecordPlanItemDecisionAsync(
        Guid userId,
        RecordPlanItemDecisionCommand command,
        CancellationToken cancellationToken);
}
