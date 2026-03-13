using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.TreatmentTypes;
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
public class TreatmentTypesController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TreatmentTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TreatmentTypeResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var types = await dbContext.TreatmentTypes
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(types);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    [ProducesResponseType(typeof(TreatmentTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentTypeResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateTreatmentTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return BadRequest(new Message("Treatment type name is required."));
        }

        var exists = await dbContext.TreatmentTypes
            .AsNoTracking()
            .AnyAsync(entity => entity.Name == name, cancellationToken);
        if (exists)
        {
            return BadRequest(new Message("Treatment type with same name already exists."));
        }

        var type = new TreatmentType
        {
            Name = name,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
            BasePrice = request.BasePrice,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        dbContext.TreatmentTypes.Add(type);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, ToResponse(type));
    }

    [HttpPut("{treatmentTypeId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    [ProducesResponseType(typeof(TreatmentTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentTypeResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid treatmentTypeId,
        [FromBody] UpdateTreatmentTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var type = await dbContext.TreatmentTypes
            .SingleOrDefaultAsync(entity => entity.Id == treatmentTypeId, cancellationToken);
        if (type == null)
        {
            return NotFound(new Message("Treatment type not found."));
        }

        type.Name = request.Name.Trim();
        type.DefaultDurationMinutes = request.DefaultDurationMinutes;
        type.BasePrice = request.BasePrice;
        type.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(type));
    }

    [HttpDelete("{treatmentTypeId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid treatmentTypeId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var type = await dbContext.TreatmentTypes
            .SingleOrDefaultAsync(entity => entity.Id == treatmentTypeId, cancellationToken);
        if (type == null)
        {
            return NotFound(new Message("Treatment type not found."));
        }

        dbContext.TreatmentTypes.Remove(type);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static TreatmentTypeResponse ToResponse(TreatmentType entity)
    {
        return new TreatmentTypeResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            DefaultDurationMinutes = entity.DefaultDurationMinutes,
            BasePrice = entity.BasePrice,
            Description = entity.Description
        };
    }
}
