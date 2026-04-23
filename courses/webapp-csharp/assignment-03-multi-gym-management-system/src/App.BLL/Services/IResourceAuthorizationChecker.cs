using App.Domain.Entities;

namespace App.BLL.Services;

public interface IResourceAuthorizationChecker
{
    Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default);
    Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default);
    Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default);
}
