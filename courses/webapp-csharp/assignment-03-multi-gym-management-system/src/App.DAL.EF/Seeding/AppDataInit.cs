using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    public const string DefaultPassword = "GymStrong123!";

    public static async Task SeedAsync(
        AppDbContext context,
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager)
    {
        await SeedRolesAsync(roleManager);
        await EnsureDemoUsersAsync(userManager);

        if (await context.Gyms.AnyAsync())
        {
            return;
        }

        await SeedRichDemoDataAsync(context, userManager);
    }

    private static async Task EnsureDemoUsersAsync(UserManager<AppUser> userManager)
    {
        var emails = new[]
        {
            "systemadmin@gym.local",
            "admin@peakforge.local",
            "member@peakforge.local",
            "trainer@peakforge.local",
            "caretaker@peakforge.local",
            "multigym.admin@gym.local",
            "support@gym.local",
            "billing@gym.local",
        };

        foreach (var email in emails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) continue;

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, DefaultPassword);
        }
    }
}
