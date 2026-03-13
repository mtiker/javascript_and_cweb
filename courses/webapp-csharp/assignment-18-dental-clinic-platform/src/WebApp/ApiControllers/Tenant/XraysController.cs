using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.Xrays;
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
public class XraysController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<XrayResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<XrayResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var query = dbContext.Xrays
            .AsNoTracking()
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        var xrays = await query
            .OrderByDescending(entity => entity.TakenAtUtc)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(xrays);
    }

    [HttpPost]
    [ProducesResponseType(typeof(XrayResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<XrayResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateXrayRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            return BadRequest(new Message("Patient does not exist in tenant."));
        }

        var xray = new Xray
        {
            PatientId = request.PatientId,
            TakenAtUtc = request.TakenAtUtc,
            NextDueAtUtc = request.NextDueAtUtc,
            StoragePath = request.StoragePath.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        dbContext.Xrays.Add(xray);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, ToResponse(xray));
    }

    [HttpDelete("{xrayId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid xrayId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var xray = await dbContext.Xrays
            .SingleOrDefaultAsync(entity => entity.Id == xrayId, cancellationToken);
        if (xray == null)
        {
            return NotFound(new Message("X-ray record not found."));
        }

        dbContext.Xrays.Remove(xray);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static XrayResponse ToResponse(Xray entity)
    {
        return new XrayResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            TakenAtUtc = entity.TakenAtUtc,
            NextDueAtUtc = entity.NextDueAtUtc,
            StoragePath = entity.StoragePath,
            Notes = entity.Notes
        };
    }
}
