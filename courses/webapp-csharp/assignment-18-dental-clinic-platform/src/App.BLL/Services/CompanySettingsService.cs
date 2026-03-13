using App.BLL.Contracts.CompanySettings;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CompanySettingsService(
    AppDbContext dbContext,
    ITenantAccessService tenantAccessService,
    ITenantProvider tenantProvider)
    : ICompanySettingsService
{
    public async Task<CompanySettingsResult> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureOwnerAccessAsync(userId, cancellationToken);
        var companyId = RequireCompanyId();

        var settings = await dbContext.CompanySettings
            .SingleOrDefaultAsync(entity => entity.CompanyId == companyId, cancellationToken);

        if (settings == null)
        {
            settings = new CompanySettings
            {
                CompanyId = companyId
            };
            dbContext.CompanySettings.Add(settings);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToResult(settings);
    }

    public async Task<CompanySettingsResult> UpdateAsync(Guid userId, UpdateCompanySettingsCommand command, CancellationToken cancellationToken)
    {
        await EnsureOwnerAccessAsync(userId, cancellationToken);
        var companyId = RequireCompanyId();

        Validate(command);

        var settings = await dbContext.CompanySettings
            .SingleOrDefaultAsync(entity => entity.CompanyId == companyId, cancellationToken);

        if (settings == null)
        {
            settings = new CompanySettings
            {
                CompanyId = companyId
            };
            dbContext.CompanySettings.Add(settings);
        }

        settings.CountryCode = command.CountryCode.Trim().ToUpperInvariant();
        settings.CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant();
        settings.Timezone = command.Timezone.Trim();
        settings.DefaultXrayIntervalMonths = command.DefaultXrayIntervalMonths;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(settings);
    }

    private Task EnsureOwnerAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner);
    }

    private Guid RequireCompanyId()
    {
        if (!tenantProvider.CompanyId.HasValue)
        {
            throw new ForbiddenException("Active tenant context is missing.");
        }

        return tenantProvider.CompanyId.Value;
    }

    private static void Validate(UpdateCompanySettingsCommand command)
    {
        var countryCode = command.CountryCode.Trim();
        if (countryCode.Length != 2 || !countryCode.All(char.IsLetter))
        {
            throw new ValidationAppException("Country code must be exactly 2 letters.");
        }

        var currencyCode = command.CurrencyCode.Trim();
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsLetter))
        {
            throw new ValidationAppException("Currency code must be exactly 3 letters.");
        }

        if (string.IsNullOrWhiteSpace(command.Timezone) || command.Timezone.Trim().Length > 128)
        {
            throw new ValidationAppException("Timezone is required and must be at most 128 characters.");
        }

        if (command.DefaultXrayIntervalMonths is < 3 or > 60)
        {
            throw new ValidationAppException("Default X-ray interval must be between 3 and 60 months.");
        }
    }

    private static CompanySettingsResult ToResult(CompanySettings settings)
    {
        return new CompanySettingsResult(
            settings.CompanyId,
            settings.CountryCode,
            settings.CurrencyCode,
            settings.Timezone,
            settings.DefaultXrayIntervalMonths);
    }
}
