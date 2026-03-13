using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CompanyOnboardingService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager)
    : ICompanyOnboardingService
{
    public async Task<RegisterCompanyResult> RegisterCompanyAsync(
        RegisterCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = command.CompanySlug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            throw new ValidationAppException("Company slug is required.");
        }

        var slugTaken = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(company => company.Slug == normalizedSlug, cancellationToken);

        if (slugTaken)
        {
            throw new ValidationAppException("Company slug is already in use.");
        }

        var company = new Company
        {
            Name = command.CompanyName.Trim(),
            Slug = normalizedSlug
        };

        var settings = new CompanySettings
        {
            CompanyId = company.Id,
            CountryCode = command.CountryCode.Trim().ToUpperInvariant()
        };

        var subscription = new Subscription
        {
            CompanyId = company.Id,
            Tier = SubscriptionTier.Free,
            Status = SubscriptionStatus.Active,
            UserLimit = 5,
            EntityLimit = 100
        };

        var ownerUser = await userManager.FindByEmailAsync(command.OwnerEmail.Trim());
        if (ownerUser == null)
        {
            ownerUser = new AppUser
            {
                Email = command.OwnerEmail.Trim(),
                UserName = command.OwnerEmail.Trim(),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(ownerUser, command.OwnerPassword);
            if (!createResult.Succeeded)
            {
                var details = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new ValidationAppException($"Could not create owner user: {details}");
            }
        }

        await EnsureRolesExistAsync(cancellationToken);

        var ownerRoleLinkExists = await dbContext.AppUserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(link =>
                    link.AppUserId == ownerUser.Id &&
                    link.CompanyId == company.Id &&
                    link.RoleName == RoleNames.CompanyOwner,
                cancellationToken);

        dbContext.Companies.Add(company);
        dbContext.CompanySettings.Add(settings);
        dbContext.Subscriptions.Add(subscription);

        if (!ownerRoleLinkExists)
        {
            dbContext.AppUserRoles.Add(new AppUserRole
            {
                AppUserId = ownerUser.Id,
                CompanyId = company.Id,
                RoleName = RoleNames.CompanyOwner,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterCompanyResult(
            company.Id,
            ownerUser.Id,
            company.Slug,
            subscription.Tier.ToString());
    }

    private async Task EnsureRolesExistAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var createResult = await roleManager.CreateAsync(new AppRole { Name = roleName });
            if (!createResult.Succeeded)
            {
                var details = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new ValidationAppException($"Could not create role '{roleName}': {details}");
            }
        }
    }
}
