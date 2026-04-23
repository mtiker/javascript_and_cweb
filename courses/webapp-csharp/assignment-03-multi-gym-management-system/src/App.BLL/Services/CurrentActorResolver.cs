using App.BLL.Contracts.Infrastructure;
using App.Domain;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CurrentActorResolver(
    IAppDbContext dbContext,
    IUserContextService userContextService) : ICurrentActorResolver
{
    public UserExecutionContext GetCurrent()
    {
        return userContextService.GetCurrent();
    }

    public async Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Members.FirstOrDefaultAsync(
            member => member.GymId == gymId && member.PersonId == context.PersonId,
            cancellationToken);
    }

    public async Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Staff.FirstOrDefaultAsync(
            staff => staff.GymId == gymId && staff.PersonId == context.PersonId,
            cancellationToken);
    }

    public bool HasTenantAdminPrivileges(UserExecutionContext context)
    {
        return context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin);
    }
}
