using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private const string PrimaryCompanySlug = "smileworks-demo";
    private const string SecondaryCompanySlug = "nordic-smiles-demo";

    private const string OwnerEmail = "owner.demo@dental-saas.local";
    private const string AdminEmail = "admin.demo@dental-saas.local";
    private const string ManagerEmail = "manager.demo@dental-saas.local";
    private const string EmployeeEmail = "employee.demo@dental-saas.local";
    private const string MultiTenantEmail = "multitenant.demo@dental-saas.local";

    public static async Task SeedAppDataAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var primaryCompany = EnsureCompany(
            context,
            name: "SmileWorks Demo Clinic",
            slug: PrimaryCompanySlug);

        var secondaryCompany = EnsureCompany(
            context,
            name: "Nordic Smiles Demo Clinic",
            slug: SecondaryCompanySlug);

        EnsureCompanySettings(context, primaryCompany.Id, "DE", "EUR", "Europe/Berlin");
        EnsureCompanySettings(context, secondaryCompany.Id, "EE", "EUR", "Europe/Tallinn");

        EnsureActiveSubscription(context, primaryCompany.Id, SubscriptionTier.Premium, 50, 5000);
        EnsureActiveSubscription(context, secondaryCompany.Id, SubscriptionTier.Standard, 20, 1000);

        var usersByEmail = context.Users
            .AsNoTracking()
            .Where(entity => entity.Email != null)
            .ToDictionary(entity => entity.Email!.ToLowerInvariant(), entity => entity);

        var missingUsers = new[] { OwnerEmail, AdminEmail, ManagerEmail, EmployeeEmail, MultiTenantEmail }
            .Where(email => !usersByEmail.ContainsKey(email))
            .ToArray();

        if (missingUsers.Length > 0)
        {
            throw new ApplicationException(
                "Seed identity users are missing. Ensure DataInitialization:SeedIdentity=true. Missing: " +
                string.Join(", ", missingUsers));
        }

        EnsureCompanyRoleLink(context, usersByEmail[OwnerEmail].Id, primaryCompany.Id, RoleNames.CompanyOwner);
        EnsureCompanyRoleLink(context, usersByEmail[AdminEmail].Id, primaryCompany.Id, RoleNames.CompanyAdmin);
        EnsureCompanyRoleLink(context, usersByEmail[ManagerEmail].Id, primaryCompany.Id, RoleNames.CompanyManager);
        EnsureCompanyRoleLink(context, usersByEmail[EmployeeEmail].Id, primaryCompany.Id, RoleNames.CompanyEmployee);
        EnsureCompanyRoleLink(context, usersByEmail[MultiTenantEmail].Id, primaryCompany.Id, RoleNames.CompanyAdmin);
        EnsureCompanyRoleLink(context, usersByEmail[MultiTenantEmail].Id, secondaryCompany.Id, RoleNames.CompanyManager);
        EnsureCompanyRoleLink(context, usersByEmail[OwnerEmail].Id, secondaryCompany.Id, RoleNames.CompanyOwner);

        await context.SaveChangesAsync(cancellationToken);

        await SeedPrimaryCompanyDataAsync(context, primaryCompany.Id, cancellationToken);
        await SeedSecondaryCompanyDataAsync(context, secondaryCompany.Id, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static async Task SeedIdentityAsync(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        bool resetSeedUserPasswords = false)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new AppRole { Name = roleName });
            if (!roleResult.Succeeded)
            {
                throw new ApplicationException($"Role creation failed for role '{roleName}'.");
            }
        }

        foreach (var (email, password, roles) in InitialData.Users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new ApplicationException($"User creation failed for '{email}'.");
                }
            }
            else
            {
                await SyncSeedUserAsync(userManager, user, email, password, resetSeedUserPasswords);
            }

            foreach (var role in roles)
            {
                if (await userManager.IsInRoleAsync(user, role))
                {
                    continue;
                }

                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new ApplicationException($"Assigning role '{role}' to '{email}' failed.");
                }
            }
        }
    }

    private static async Task SyncSeedUserAsync(
        UserManager<AppUser> userManager,
        AppUser user,
        string email,
        string password,
        bool resetSeedUserPasswords)
    {
        var requiresUserUpdate = false;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            requiresUserUpdate = true;
        }

        if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = email;
            requiresUserUpdate = true;
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            requiresUserUpdate = true;
        }

        if (requiresUserUpdate)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ApplicationException($"Updating seeded user '{email}' failed.");
            }
        }

        if (!resetSeedUserPasswords || await userManager.CheckPasswordAsync(user, password))
        {
            return;
        }

        IdentityResult passwordResult;
        if (await userManager.HasPasswordAsync(user))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            passwordResult = await userManager.ResetPasswordAsync(user, resetToken, password);
        }
        else
        {
            passwordResult = await userManager.AddPasswordAsync(user, password);
        }

        if (!passwordResult.Succeeded)
        {
            throw new ApplicationException($"Resetting seeded user password for '{email}' failed.");
        }
    }

    private static Company EnsureCompany(AppDbContext context, string name, string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var company = context.Companies
            .IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.Slug == normalizedSlug);

        if (company != null)
        {
            return company;
        }

        company = new Company
        {
            Name = name.Trim(),
            Slug = normalizedSlug,
            IsActive = true
        };

        context.Companies.Add(company);
        return company;
    }

    private static void EnsureCompanySettings(
        AppDbContext context,
        Guid companyId,
        string countryCode,
        string currencyCode,
        string timezone)
    {
        var settingsExists = context.CompanySettings
            .IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId);

        if (settingsExists)
        {
            return;
        }

        context.CompanySettings.Add(new CompanySettings
        {
            CompanyId = companyId,
            CountryCode = countryCode,
            CurrencyCode = currencyCode,
            Timezone = timezone,
            DefaultXrayIntervalMonths = 12
        });
    }

    private static void EnsureActiveSubscription(
        AppDbContext context,
        Guid companyId,
        SubscriptionTier tier,
        int userLimit,
        int entityLimit)
    {
        var hasActiveSubscription = context.Subscriptions
            .IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId && entity.Status == SubscriptionStatus.Active);

        if (hasActiveSubscription)
        {
            return;
        }

        context.Subscriptions.Add(new Subscription
        {
            CompanyId = companyId,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            StartsAtUtc = DateTime.UtcNow.AddMonths(-1),
            UserLimit = userLimit,
            EntityLimit = entityLimit
        });
    }

    private static void EnsureCompanyRoleLink(AppDbContext context, Guid appUserId, Guid companyId, string roleName)
    {
        var linkExists = context.AppUserRoles
            .IgnoreQueryFilters()
            .Any(entity =>
                entity.AppUserId == appUserId &&
                entity.CompanyId == companyId &&
                entity.RoleName == roleName);

        if (linkExists)
        {
            return;
        }

        context.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = appUserId,
            CompanyId = companyId,
            RoleName = roleName,
            IsActive = true
        });
    }

    private static async Task SeedPrimaryCompanyDataAsync(AppDbContext context, Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await SeedClinicDataAsync(context, companyId, BuildPrimaryClinicSeed(now), cancellationToken);
        await EnsurePrimaryFinancialArtifactsAsync(context, companyId, now, cancellationToken);
    }

    private static async Task SeedSecondaryCompanyDataAsync(AppDbContext context, Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await SeedClinicDataAsync(context, companyId, BuildSecondaryClinicSeed(now), cancellationToken);
        await EnsureSecondaryFinancialArtifactsAsync(context, companyId, now, cancellationToken);
    }
}
