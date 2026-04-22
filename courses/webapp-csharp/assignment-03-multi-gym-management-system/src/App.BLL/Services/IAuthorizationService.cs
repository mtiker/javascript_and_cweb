using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface IAuthorizationService
{
    Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles);
    Task<Member?> GetCurrentMemberAsync(Guid gymId);
    Task<Staff?> GetCurrentStaffAsync(Guid gymId);
    Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId);
    Task EnsureBookingAccessAsync(Booking booking);
    Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession);
    Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task);
}
