using App.DAL.EF;
using App.DAL.EF.Seeding;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Setup;

public static class AppDataInitExtensions
{
    public static async Task SetupAppDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();
        var dbContext = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<App.Domain.Identity.AppRole>>();
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<App.Domain.Identity.AppUser>>();

        var shouldMigrate = configuration.GetValue("DataInitialization:MigrateDatabase", true);
        var shouldSeed = configuration.GetValue("DataInitialization:SeedData", true);

        if (shouldMigrate && dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        if (!shouldSeed)
        {
            return;
        }

        await AppDataInit.SeedAsync(dbContext, roleManager, userManager);
    }
}
