using App.BLL.Exceptions;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class TenantAccessService(AppDbContext dbContext) : ITenantAccessService
{
    public async Task EnsureCompanyRoleAsync(Guid userId, CancellationToken cancellationToken, params string[] requiredRoles)
    {
        if (requiredRoles.Length == 0)
        {
            throw new ValidationAppException("At least one required role must be specified.");
        }

        var hasRole = await dbContext.AppUserRoles
            .AsNoTracking()
            .AnyAsync(role =>
                    role.AppUserId == userId &&
                    role.IsActive &&
                    requiredRoles.Contains(role.RoleName),
                cancellationToken);

        if (!hasRole)
        {
            throw new ForbiddenException("Current user does not have required company role.");
        }
    }
}
