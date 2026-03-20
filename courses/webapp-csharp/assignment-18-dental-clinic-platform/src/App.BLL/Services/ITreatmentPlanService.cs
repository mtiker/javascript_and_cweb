using App.BLL.Contracts;
using App.BLL.Contracts.TreatmentPlans;

namespace App.BLL.Services;

public interface ITreatmentPlanService
{
    Task<IReadOnlyCollection<TreatmentPlanResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken);
    Task<TreatmentPlanResult> GetAsync(Guid userId, Guid planId, CancellationToken cancellationToken);
    Task<TreatmentPlanResult> CreateAsync(Guid userId, CreateTreatmentPlanCommand command, CancellationToken cancellationToken);
    Task<TreatmentPlanResult> UpdateAsync(Guid userId, UpdateTreatmentPlanCommand command, CancellationToken cancellationToken);
    Task<SubmitTreatmentPlanResult> SubmitAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid planId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OpenPlanItemResult>> ListOpenItemsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PlanDecisionResult> RecordPlanItemDecisionAsync(
        Guid userId,
        RecordPlanItemDecisionCommand command,
        CancellationToken cancellationToken);
}
