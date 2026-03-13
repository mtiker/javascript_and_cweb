using App.BLL.Exceptions;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestTenantAccessService
{
    [Fact]
    public async Task EnsureCompanyRoleAsync_ThrowsForbidden_WhenRoleMissing()
    {
        var tenantProvider = new TestTenantProvider();
        tenantProvider.SetTenant(Guid.NewGuid(), "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"tenant-role-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = tenantProvider.CompanyId!.Value,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var service = new TenantAccessService(db);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.EnsureCompanyRoleAsync(userId, CancellationToken.None, RoleNames.CompanyManager));
    }
}
