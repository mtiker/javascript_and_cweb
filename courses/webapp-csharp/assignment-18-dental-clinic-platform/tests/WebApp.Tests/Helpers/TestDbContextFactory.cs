using App.DAL.EF;
using App.DAL.EF.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string dbName, ITenantProvider tenantProvider, IHttpContextAccessor? httpContextAccessor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options, tenantProvider, httpContextAccessor ?? new HttpContextAccessor());
    }
}
