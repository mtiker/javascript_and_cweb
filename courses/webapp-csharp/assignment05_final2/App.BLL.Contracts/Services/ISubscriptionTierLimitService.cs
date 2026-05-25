namespace App.BLL.Contracts.Services;

public interface ISubscriptionTierLimitService
{
    Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default);
}
