using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static Person CreatePerson(string firstName, string lastName, string personalCode)
    {
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            PersonalCode = personalCode
        };
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string displayName,
        Person? person)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            await userManager.ResetPasswordAsync(existingUser, token, DefaultPassword);
            return existingUser;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Person = person
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create seed user '{email}': {errors}");
        }

        return user;
    }

    private static async Task EnsureRoleMembershipAsync(UserManager<AppUser> userManager, AppUser user, string roleName)
    {
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in RoleNames.SystemRoles.Concat(RoleNames.TenantRoles))
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await roleManager.CreateAsync(new AppRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });
        }
    }
}
