using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void SeedAppData(AppDbContext context)
    {
        if (!context.Companies.Any())
        {
            // Domain sample data is intentionally minimal in skeleton.
        }
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = roleManager.FindByNameAsync(roleName).Result;
            if (role != null)
            {
                continue;
            }

            var roleResult = roleManager.CreateAsync(new AppRole { Name = roleName }).Result;
            if (!roleResult.Succeeded)
            {
                throw new ApplicationException($"Role creation failed for role '{roleName}'.");
            }
        }

        foreach (var (email, password, roles) in InitialData.Users)
        {
            var user = userManager.FindByEmailAsync(email).Result;
            if (user == null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createResult = userManager.CreateAsync(user, password).Result;
                if (!createResult.Succeeded)
                {
                    throw new ApplicationException($"User creation failed for '{email}'.");
                }
            }

            foreach (var role in roles)
            {
                if (userManager.IsInRoleAsync(user, role).Result)
                {
                    continue;
                }

                var roleResult = userManager.AddToRoleAsync(user, role).Result;
                if (!roleResult.Succeeded)
                {
                    throw new ApplicationException($"Assigning role '{role}' to '{email}' failed.");
                }
            }
        }
    }
}
