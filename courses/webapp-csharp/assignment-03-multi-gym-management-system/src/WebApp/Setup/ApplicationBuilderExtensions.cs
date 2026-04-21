using App.DAL.EF;
using App.DAL.EF.Seeding;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using WebApp.Middleware;

namespace WebApp.Setup;

public static class ApplicationBuilderExtensions
{
    public static async Task SeedAppAsync(this WebApplication app)
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

    public static void UseAppPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseMiddleware<ProblemDetailsMiddleware>();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("ClientApp");
        app.UseRequestLocalization();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
        var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var clientIndexPath = Path.Combine(webRootPath, "client", "index.html");
        app.MapGet("/client", () => Results.File(clientIndexPath, "text/html"));
        app.MapFallbackToFile("/client/{*path:nonfile}", "client/index.html");
        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    }
}
