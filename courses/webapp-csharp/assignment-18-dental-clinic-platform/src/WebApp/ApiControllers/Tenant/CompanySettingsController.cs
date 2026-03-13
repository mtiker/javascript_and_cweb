using App.BLL.Contracts.CompanySettings;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1.CompanySettings;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner)]
public class CompanySettingsController(
    ICompanySettingsService companySettingsService,
    ITenantProvider tenantProvider)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CompanySettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySettingsResponse>> Get([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var result = await companySettingsService.GetAsync(User.UserId(), cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPut]
    [ProducesResponseType(typeof(CompanySettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanySettingsResponse>> Update(
        [FromRoute] string companySlug,
        [FromBody] UpdateCompanySettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var result = await companySettingsService.UpdateAsync(
            User.UserId(),
            new UpdateCompanySettingsCommand(
                request.CountryCode,
                request.CurrencyCode,
                request.Timezone,
                request.DefaultXrayIntervalMonths),
            cancellationToken);

        return Ok(ToResponse(result));
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static CompanySettingsResponse ToResponse(CompanySettingsResult result)
    {
        return new CompanySettingsResponse
        {
            CompanyId = result.CompanyId,
            CountryCode = result.CountryCode,
            CurrencyCode = result.CurrencyCode,
            Timezone = result.Timezone,
            DefaultXrayIntervalMonths = result.DefaultXrayIntervalMonths
        };
    }
}
