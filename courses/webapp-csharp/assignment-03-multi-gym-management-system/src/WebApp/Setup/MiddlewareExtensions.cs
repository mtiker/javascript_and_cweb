using Microsoft.AspNetCore.Localization;
using WebApp.Middleware;

namespace WebApp.Setup;

public static class MiddlewareExtensions
{
    public static WebApplication UseAppPipeline(this WebApplication app)
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

        return app;
    }

    public static WebApplication UseAppSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapControllers();

        var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var clientIndexPath = Path.Combine(webRootPath, "client", "index.html");
        app.MapGet("/client", () => Results.File(clientIndexPath, "text/html"));
        app.MapFallbackToFile("/client/{*path:nonfile}", "client/index.html");
        app.MapAreaControllerRoute(
            name: "mvc-client",
            areaName: "Client",
            pattern: "mvc-client/{controller=Dashboard}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
