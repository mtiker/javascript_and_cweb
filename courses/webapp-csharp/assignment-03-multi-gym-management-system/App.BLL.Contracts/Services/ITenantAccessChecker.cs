namespace App.BLL.Contracts.Services;

public interface ITenantAccessChecker
{
    Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles);
}
