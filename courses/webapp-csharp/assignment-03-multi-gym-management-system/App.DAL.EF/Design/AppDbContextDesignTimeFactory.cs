using App.DAL.EF.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.DAL.EF.Design;

public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("WebApp/appsettings.json", optional: true)
            .AddJsonFile("WebApp/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=multi_gym_management_system;Username=postgres;Password=postgres";

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);

        return new AppDbContext(builder.Options, new DesignTimeGymContext());
    }

    private sealed class DesignTimeGymContext : IGymContext
    {
        public Guid? GymId => null;
        public string? GymCode => null;
        public string? ActiveRole => null;
        public bool IgnoreGymFilter => true;
    }
}
