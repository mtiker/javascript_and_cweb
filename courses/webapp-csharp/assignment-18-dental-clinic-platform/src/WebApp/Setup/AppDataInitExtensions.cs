using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WebApp.Setup;

public static class AppDataInitExtensions
{
    public static async Task SetupAppDataAsync(this WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        var configuration = app.Configuration;
        WaitForDatabaseConnection(context, logger, configuration);

        using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase");
            AppDataInit.DeleteDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase");
            AppDataInit.MigrateDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
        {
            logger.LogInformation("SeedIdentity");
            await AppDataInit.SeedIdentityAsync(
                userManager,
                roleManager,
                configuration.GetValue<bool>("DataInitialization:ResetSeedUserPasswords"));
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedData"))
        {
            logger.LogInformation("SeedData");
            await AppDataInit.SeedAppDataAsync(context);
        }
    }

    private static void WaitForDatabaseConnection(
        AppDbContext context,
        ILogger<IApplicationBuilder> logger,
        IConfiguration configuration)
    {
        var maxWaitSeconds = Math.Max(1, configuration.GetValue<int>("DataInitialization:DbConnectTimeoutSeconds", 30));
        var retryDelayMs = Math.Max(250, configuration.GetValue<int>("DataInitialization:DbConnectRetryDelayMs", 1000));
        var startTimeUtc = DateTime.UtcNow;
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                context.Database.OpenConnection();
                context.Database.CloseConnection();
                return;
            }
            catch (Npgsql.PostgresException exception)
            {
                logger.LogWarning("Postgres is not ready yet: {Message}", exception.Message);
                if (exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Thread.Sleep(retryDelayMs);
            }
            catch (Exception exception) when (IsTransientConnectionIssue(exception))
            {
                var elapsed = DateTime.UtcNow - startTimeUtc;
                if (elapsed.TotalSeconds >= maxWaitSeconds)
                {
                    throw new InvalidOperationException(
                        $"Could not connect to database within {maxWaitSeconds} seconds. " +
                        "Ensure PostgreSQL is running and ConnectionStrings:DefaultConnection is correct.",
                        exception);
                }

                logger.LogWarning(
                    exception,
                    "Database is not reachable yet (attempt {Attempt}). Retrying in {DelayMs} ms.",
                    attempt,
                    retryDelayMs);

                Thread.Sleep(retryDelayMs);
            }
        }
    }

    private static bool IsTransientConnectionIssue(Exception exception)
    {
        if (exception is NpgsqlException or System.Net.Sockets.SocketException)
        {
            return true;
        }

        if (exception is InvalidOperationException invalidOperationException &&
            invalidOperationException.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return exception.InnerException != null && IsTransientConnectionIssue(exception.InnerException);
    }
}
