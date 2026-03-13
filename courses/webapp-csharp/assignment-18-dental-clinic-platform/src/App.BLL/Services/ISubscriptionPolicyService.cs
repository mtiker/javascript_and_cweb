namespace App.BLL.Services;

public interface ISubscriptionPolicyService
{
    Task EnsureCanCreatePatientAsync(CancellationToken cancellationToken);
    Task EnsureCanAddActiveMembershipAsync(CancellationToken cancellationToken);
    Task EnsureTierAtLeastAsync(string featureName, App.Domain.Enums.SubscriptionTier minimumTier, CancellationToken cancellationToken);
}
