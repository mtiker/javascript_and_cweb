using App.Domain.Enums;

namespace App.BLL.Services;

public interface ISubscriptionTierLimitService
{
    Task<SubscriptionPlan> GetCurrentPlanAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default);
}
