using App.DAL.EF;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace WebApp.Setup;

public static class DatabaseExtensions
{
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        var enableDetailedErrors = configuration.GetValue<bool>("Diagnostics:EnableDetailedErrors", !environment.IsProduction());
        var enableSensitiveDataLogging = configuration.GetValue<bool>("Diagnostics:EnableSensitiveDataLogging", false);

#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618

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

        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

        return services;
    }
}
