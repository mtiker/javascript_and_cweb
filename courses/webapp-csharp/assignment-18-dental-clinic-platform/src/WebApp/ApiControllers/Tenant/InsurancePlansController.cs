using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.InsurancePlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager + "," + RoleNames.CompanyEmployee)]
public class InsurancePlansController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<InsurancePlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InsurancePlanResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plans = await dbContext.InsurancePlans
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(typeof(InsurancePlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InsurancePlanResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateInsurancePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (!TryParseCoverage(request.CoverageType, out var coverageType))
        {
            return BadRequest(new Message("Invalid coverage type."));
        }

        var plan = new InsurancePlan
        {
            Name = request.Name.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            CoverageType = coverageType,
            IsActivePlan = request.IsActivePlan,
            ClaimSubmissionEndpoint = string.IsNullOrWhiteSpace(request.ClaimSubmissionEndpoint) ? null : request.ClaimSubmissionEndpoint.Trim()
        };

        dbContext.InsurancePlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, ToResponse(plan));
    }

    [HttpPut("{insurancePlanId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(typeof(InsurancePlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsurancePlanResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid insurancePlanId,
        [FromBody] UpdateInsurancePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (!TryParseCoverage(request.CoverageType, out var coverageType))
        {
            return BadRequest(new Message("Invalid coverage type."));
        }

        var plan = await dbContext.InsurancePlans
            .SingleOrDefaultAsync(entity => entity.Id == insurancePlanId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Insurance plan not found."));
        }

        plan.Name = request.Name.Trim();
        plan.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        plan.CoverageType = coverageType;
        plan.IsActivePlan = request.IsActivePlan;
        plan.ClaimSubmissionEndpoint = string.IsNullOrWhiteSpace(request.ClaimSubmissionEndpoint) ? null : request.ClaimSubmissionEndpoint.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(plan));
    }

    [HttpDelete("{insurancePlanId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid insurancePlanId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await dbContext.InsurancePlans
            .SingleOrDefaultAsync(entity => entity.Id == insurancePlanId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Insurance plan not found."));
        }

        dbContext.InsurancePlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseCoverage(string value, out CoverageType coverageType)
    {
        return Enum.TryParse(value?.Trim(), true, out coverageType);
    }

    private static InsurancePlanResponse ToResponse(InsurancePlan entity)
    {
        return new InsurancePlanResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            CoverageType = entity.CoverageType.ToString(),
            IsActivePlan = entity.IsActivePlan,
            ClaimSubmissionEndpoint = entity.ClaimSubmissionEndpoint
        };
    }
}
