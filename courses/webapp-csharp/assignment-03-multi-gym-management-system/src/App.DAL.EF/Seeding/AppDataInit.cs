using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    public const string DefaultPassword = "Gym123!";

    public static async Task SeedAsync(
        AppDbContext context,
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager)
    {
        await SeedRolesAsync(roleManager);

        if (await context.Gyms.AnyAsync())
        {
            return;
        }

        await SeedRichDemoDataAsync(context, userManager);
    }
}
