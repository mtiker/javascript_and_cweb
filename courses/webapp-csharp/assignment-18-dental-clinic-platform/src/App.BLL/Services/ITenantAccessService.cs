namespace App.BLL.Services;

public interface ITenantAccessService
{
    Task EnsureCompanyRoleAsync(Guid userId, CancellationToken cancellationToken, params string[] requiredRoles);
}
