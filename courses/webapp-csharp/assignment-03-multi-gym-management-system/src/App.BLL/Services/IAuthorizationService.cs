using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface IAuthorizationService
{
    Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles);
    Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles);
    Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default);
    Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default);
    Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default);
}
