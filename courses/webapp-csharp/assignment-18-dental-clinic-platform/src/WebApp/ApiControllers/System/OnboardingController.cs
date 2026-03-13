using App.BLL.Contracts;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1.System;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]/[action]")]
[ApiController]
public class OnboardingController(ICompanyOnboardingService onboardingService, AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RegisterCompanyResponse>> RegisterCompany([FromBody] RegisterCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.RegisterCompanyAsync(
            new RegisterCompanyCommand(
                request.CompanyName,
                request.CompanySlug,
                request.OwnerEmail,
                request.OwnerPassword,
                request.CountryCode),
            cancellationToken);

        return Ok(new RegisterCompanyResponse
        {
            CompanyId = result.CompanyId,
            OwnerUserId = result.OwnerUserId,
            CompanySlug = result.CompanySlug,
            SubscriptionTier = result.SubscriptionTier
        });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.SystemAdmin + "," + RoleNames.SystemSupport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<object>>> Companies(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => new
            {
                entity.Id,
                entity.Name,
                entity.Slug,
                entity.IsActive,
                entity.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }
}
