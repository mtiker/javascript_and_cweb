using App.DAL.EF.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace App.DAL.EF.Design;

public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DENTAL_SAAS_CONNECTION_STRING") ??
            "Server=127.0.0.1;Port=5432;Database=dental_saas;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(
            optionsBuilder.Options,
            new DesignTimeTenantProvider(),
            new HttpContextAccessor());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid? CompanyId => null;
        public string? CompanySlug => null;
        public bool IgnoreTenantFilter => true;

        public void SetTenant(Guid companyId, string companySlug)
        {
        }

        public void SetIgnoreTenantFilter(bool ignore)
        {
        }

        public void ClearTenant()
        {
        }
    }
}
