using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.System.Platform;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Helpers;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/platform")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.SystemAdmin)]
public class PlatformController(AppDbContext dbContext, IFeatureFlagStore featureFlagStore) : ControllerBase
{
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(PlatformAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformAnalyticsResponse>> Analytics(CancellationToken cancellationToken)
    {
        var response = new PlatformAnalyticsResponse
        {
            CompanyCount = await dbContext.Companies.IgnoreQueryFilters().CountAsync(cancellationToken),
            ActiveCompanyCount = await dbContext.Companies.IgnoreQueryFilters().CountAsync(entity => entity.IsActive, cancellationToken),
            UserCount = await dbContext.Users.IgnoreQueryFilters().CountAsync(cancellationToken),
            PatientCount = await dbContext.Patients.IgnoreQueryFilters().CountAsync(entity => !entity.IsDeleted, cancellationToken),
            AppointmentCount = await dbContext.Appointments.IgnoreQueryFilters().CountAsync(entity => !entity.IsDeleted, cancellationToken),
            InvoiceCount = await dbContext.Invoices.IgnoreQueryFilters().CountAsync(entity => !entity.IsDeleted, cancellationToken)
        };

        return Ok(response);
    }

    [HttpGet("featureflags")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FeatureFlagResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<FeatureFlagResponse>> FeatureFlags()
    {
        var flags = featureFlagStore.GetAll()
            .Select(entity => new FeatureFlagResponse
            {
                Key = entity.Key,
                Enabled = entity.Value
            })
            .ToList();

        return Ok(flags);
    }

    [HttpPut("featureflags")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FeatureFlagResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<FeatureFlagResponse>> UpdateFeatureFlag([FromBody] UpdateFeatureFlagRequest request)
    {
        featureFlagStore.Set(request.Key, request.Enabled);

        var flags = featureFlagStore.GetAll()
            .Select(entity => new FeatureFlagResponse
            {
                Key = entity.Key,
                Enabled = entity.Value
            })
            .ToList();

        return Ok(flags);
    }

    [HttpPut("companies/{companyId:guid}/activation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompanyActivation(
        [FromRoute] Guid companyId,
        [FromBody] UpdateCompanyActivationRequest request,
        CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.Id == companyId, cancellationToken);
        if (company == null)
        {
            return NotFound(new Message("Company not found."));
        }

        company.IsActive = request.IsActive;
        company.DeactivatedAtUtc = request.IsActive ? null : DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
