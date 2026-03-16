using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.TreatmentRooms;
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
public class TreatmentRoomsController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TreatmentRoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TreatmentRoomResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var rooms = await dbContext.TreatmentRooms
            .AsNoTracking()
            .OrderBy(entity => entity.Code)
            .ThenBy(entity => entity.Name)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(rooms);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(typeof(TreatmentRoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentRoomResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateTreatmentRoomRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new Message("Room name and code are required."));
        }

        var exists = await dbContext.TreatmentRooms
            .AsNoTracking()
            .AnyAsync(entity => entity.Code == code, cancellationToken);
        if (exists)
        {
            return BadRequest(new Message("Treatment room code is already in use."));
        }

        var room = new TreatmentRoom
        {
            Name = name,
            Code = code,
            IsActiveRoom = request.IsActiveRoom
        };

        dbContext.TreatmentRooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(room);
        return CreatedAtAction(nameof(List), new { version = "1", companySlug }, response);
    }

    [HttpPut("{roomId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(typeof(TreatmentRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentRoomResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid roomId,
        [FromBody] CreateTreatmentRoomRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var room = await dbContext.TreatmentRooms
            .SingleOrDefaultAsync(entity => entity.Id == roomId, cancellationToken);
        if (room == null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new Message("Room name and code are required."));
        }

        var exists = await dbContext.TreatmentRooms
            .AsNoTracking()
            .AnyAsync(entity => entity.Id != roomId && entity.Code == code, cancellationToken);
        if (exists)
        {
            return BadRequest(new Message("Treatment room code is already in use."));
        }

        room.Name = name;
        room.Code = code;
        room.IsActiveRoom = request.IsActiveRoom;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(room));
    }

    [HttpDelete("{roomId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] string companySlug,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var room = await dbContext.TreatmentRooms
            .SingleOrDefaultAsync(entity => entity.Id == roomId, cancellationToken);
        if (room == null)
        {
            return NotFound();
        }

        dbContext.TreatmentRooms.Remove(room);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static TreatmentRoomResponse ToResponse(TreatmentRoom entity)
    {
        return new TreatmentRoomResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            IsActiveRoom = entity.IsActiveRoom
        };
    }
}
