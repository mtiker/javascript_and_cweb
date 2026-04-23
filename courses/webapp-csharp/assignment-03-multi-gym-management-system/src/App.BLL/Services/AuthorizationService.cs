using App.Domain.Entities;

namespace App.BLL.Services;

public class AuthorizationService(
    ICurrentActorResolver currentActorResolver,
    ITenantAccessChecker tenantAccessChecker,
    IResourceAuthorizationChecker resourceAuthorizationChecker) : IAuthorizationService
{
    public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles)
    {
        return EnsureTenantAccessAsync(gymCode, CancellationToken.None, allowedRoles);
    }

    public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles)
    {
        return tenantAccessChecker.EnsureTenantAccessAsync(gymCode, cancellationToken, allowedRoles);
    }

    public Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return currentActorResolver.GetCurrentMemberAsync(gymId, cancellationToken);
    }

    public Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return currentActorResolver.GetCurrentStaffAsync(gymId, cancellationToken);
    }

    public Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return resourceAuthorizationChecker.EnsureMemberSelfAccessAsync(gymId, memberId, cancellationToken);
    }

    public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return resourceAuthorizationChecker.EnsureBookingAccessAsync(booking, cancellationToken);
    }

    public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default)
    {
        return resourceAuthorizationChecker.EnsureTrainingAttendanceAccessAsync(trainingSession, cancellationToken);
    }

    public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default)
    {
        return resourceAuthorizationChecker.EnsureMaintenanceTaskAccessAsync(task, cancellationToken);
    }
}
