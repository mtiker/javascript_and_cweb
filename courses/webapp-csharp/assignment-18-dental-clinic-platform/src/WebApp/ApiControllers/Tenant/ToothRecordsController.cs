using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.ToothRecords;
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
public class ToothRecordsController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ToothRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ToothRecordResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var query = dbContext.ToothRecords
            .AsNoTracking()
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        var records = await query
            .OrderBy(entity => entity.PatientId)
            .ThenBy(entity => entity.ToothNumber)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ToothRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ToothRecordResponse>> Upsert(
        [FromRoute] string companySlug,
        [FromBody] UpsertToothRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (!Enum.TryParse<ToothConditionStatus>(request.Condition, true, out var condition))
        {
            return BadRequest(new Message("Invalid tooth condition value."));
        }

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            return BadRequest(new Message("Patient does not exist in tenant."));
        }

        var record = await dbContext.ToothRecords
            .SingleOrDefaultAsync(entity =>
                    entity.PatientId == request.PatientId &&
                    entity.ToothNumber == request.ToothNumber,
                cancellationToken);

        if (record == null)
        {
            record = new ToothRecord
            {
                PatientId = request.PatientId,
                ToothNumber = request.ToothNumber,
                Condition = condition,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };
            dbContext.ToothRecords.Add(record);
        }
        else
        {
            record.Condition = condition;
            record.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(record));
    }

    [HttpDelete("{toothRecordId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid toothRecordId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var record = await dbContext.ToothRecords
            .SingleOrDefaultAsync(entity => entity.Id == toothRecordId, cancellationToken);
        if (record == null)
        {
            return NotFound(new Message("Tooth record not found."));
        }

        dbContext.ToothRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static ToothRecordResponse ToResponse(ToothRecord entity)
    {
        return new ToothRecordResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            ToothNumber = entity.ToothNumber,
            Condition = entity.Condition.ToString(),
            Notes = entity.Notes
        };
    }
}
