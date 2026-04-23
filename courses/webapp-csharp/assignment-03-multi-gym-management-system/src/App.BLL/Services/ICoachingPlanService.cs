using App.DTO.v1.CoachingPlans;

namespace App.BLL.Services;

public interface ICoachingPlanService
{
    Task<IReadOnlyCollection<CoachingPlanResponse>> GetPlansAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default);
    Task<CoachingPlanResponse> GetPlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<CoachingPlanResponse> CreatePlanAsync(string gymCode, CoachingPlanCreateRequest request, CancellationToken cancellationToken = default);
    Task<CoachingPlanResponse> UpdatePlanAsync(string gymCode, Guid id, CoachingPlanUpdateRequest request, CancellationToken cancellationToken = default);
    Task<CoachingPlanResponse> UpdatePlanStatusAsync(string gymCode, Guid id, CoachingPlanStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<CoachingPlanResponse> DecidePlanItemAsync(string gymCode, Guid id, Guid itemId, CoachingPlanItemDecisionRequest request, CancellationToken cancellationToken = default);
    Task DeletePlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
