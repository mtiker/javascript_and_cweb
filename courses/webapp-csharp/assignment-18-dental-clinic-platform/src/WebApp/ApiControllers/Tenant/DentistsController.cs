using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.Dentists;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager + "," + RoleNames.CompanyEmployee)]
public class DentistsController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DentistResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DentistResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var dentists = await dbContext.Dentists
            .AsNoTracking()
            .OrderBy(entity => entity.DisplayName)
            .ThenBy(entity => entity.LicenseNumber)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(dentists);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(typeof(DentistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DentistResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateDentistRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var displayName = request.DisplayName.Trim();
        var licenseNumber = request.LicenseNumber.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(licenseNumber))
        {
            return BadRequest(new Message("Display name and license number are required."));
        }

        var exists = await dbContext.Dentists
            .AsNoTracking()
            .AnyAsync(entity => entity.LicenseNumber == licenseNumber, cancellationToken);
        if (exists)
        {
            return BadRequest(new Message("Dentist with the same license number already exists."));
        }

        var dentist = new Dentist
        {
            DisplayName = displayName,
            LicenseNumber = licenseNumber,
            Specialty = string.IsNullOrWhiteSpace(request.Specialty) ? null : request.Specialty.Trim()
        };

        dbContext.Dentists.Add(dentist);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(dentist);
        return CreatedAtAction(nameof(List), new { version = "1", companySlug }, response);
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static DentistResponse ToResponse(Dentist entity)
    {
        return new DentistResponse
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            LicenseNumber = entity.LicenseNumber,
            Specialty = entity.Specialty
        };
    }
}
