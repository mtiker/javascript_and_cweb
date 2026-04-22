using App.BLL.Contracts.Infrastructure;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WebApp.Setup;

public static class DatabaseExtensions
{
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddScoped<IGymContext, HttpGymContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        var enableDetailedErrors = configuration.GetValue<bool>("Diagnostics:EnableDetailedErrors", environment.IsDevelopment());
        var enableSensitiveDataLogging = configuration.GetValue<bool>("Diagnostics:EnableSensitiveDataLogging", false);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                    connectionString,
                    providerOptions => providerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            if (enableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

        return services;
    }
}
