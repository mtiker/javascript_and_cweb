using App.Domain.Entities;

namespace App.BLL.Services;

public interface ICurrentActorResolver
{
    UserExecutionContext GetCurrent();
    Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default);
    bool HasTenantAdminPrivileges(UserExecutionContext context);
}
