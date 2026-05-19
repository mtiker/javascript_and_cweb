namespace App.BLL.Services;

public class SubscriptionTierLimitService : ISubscriptionTierLimitService
{
    public Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
