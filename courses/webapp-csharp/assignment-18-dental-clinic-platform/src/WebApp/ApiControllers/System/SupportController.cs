using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.System.Support;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Helpers;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/support")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.SystemAdmin + "," + RoleNames.SystemSupport)]
public class SupportController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("companies")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CompanySnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CompanySnapshotResponse>>> Companies(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => new CompanySnapshotResponse
            {
                CompanyId = entity.Id,
                CompanyName = entity.Name,
                CompanySlug = entity.Slug,
                IsActive = entity.IsActive,
                CreatedAtUtc = entity.CreatedAtUtc,
                ActiveUserCount = dbContext.AppUserRoles
                    .IgnoreQueryFilters()
                    .Count(role => role.CompanyId == entity.Id && role.IsActive),
                PatientCount = dbContext.Patients
                    .IgnoreQueryFilters()
                    .Count(patient => patient.CompanyId == entity.Id && !patient.IsDeleted),
                AppointmentCount = dbContext.Appointments
                    .IgnoreQueryFilters()
                    .Count(appointment => appointment.CompanyId == entity.Id && !appointment.IsDeleted),
                OpenInvoiceCount = dbContext.Invoices
                    .IgnoreQueryFilters()
                    .Count(invoice => invoice.CompanyId == entity.Id && !invoice.IsDeleted && invoice.BalanceAmount > 0)
            })
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpGet("companies/{companySlug}")]
    [ProducesResponseType(typeof(CompanySnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanySnapshotResponse>> CompanySnapshot([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Slug == companySlug.Trim().ToLowerInvariant(), cancellationToken);
        if (company == null)
        {
            return NotFound(new Message("Company not found."));
        }

        var snapshot = new CompanySnapshotResponse
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            CompanySlug = company.Slug,
            IsActive = company.IsActive,
            CreatedAtUtc = company.CreatedAtUtc,
            ActiveUserCount = await dbContext.AppUserRoles
                .IgnoreQueryFilters()
                .CountAsync(role => role.CompanyId == company.Id && role.IsActive, cancellationToken),
            PatientCount = await dbContext.Patients
                .IgnoreQueryFilters()
                .CountAsync(patient => patient.CompanyId == company.Id && !patient.IsDeleted, cancellationToken),
            AppointmentCount = await dbContext.Appointments
                .IgnoreQueryFilters()
                .CountAsync(appointment => appointment.CompanyId == company.Id && !appointment.IsDeleted, cancellationToken),
            OpenInvoiceCount = await dbContext.Invoices
                .IgnoreQueryFilters()
                .CountAsync(invoice => invoice.CompanyId == company.Id && !invoice.IsDeleted && invoice.BalanceAmount > 0, cancellationToken)
        };

        return Ok(snapshot);
    }

    [HttpGet("tickets")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SupportTicketResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SupportTicketResponse>>> Tickets(CancellationToken cancellationToken)
    {
        var rawTickets = await dbContext.AuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.EntityName == "SupportTicket")
            .Join(
                dbContext.Companies.IgnoreQueryFilters().AsNoTracking(),
                log => log.CompanyId,
                company => company.Id,
                (log, company) => new
                {
                    TicketId = log.EntityId,
                    CompanyId = company.Id,
                    CompanySlug = company.Slug,
                    log.ChangedAtUtc,
                    log.ChangesJson
                })
            .OrderByDescending(entity => entity.ChangedAtUtc)
            .ToListAsync(cancellationToken);

        var tickets = rawTickets.Select(entity =>
        {
            var payload = ParseTicketPayload(entity.ChangesJson);
            return new SupportTicketResponse
            {
                TicketId = entity.TicketId,
                CompanyId = entity.CompanyId,
                CompanySlug = entity.CompanySlug,
                Subject = payload.subject,
                Details = payload.details,
                Status = payload.status,
                CreatedAtUtc = entity.ChangedAtUtc
            };
        }).ToList();

        return Ok(tickets);
    }

    [HttpPost("tickets")]
    [ProducesResponseType(typeof(SupportTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportTicketResponse>> CreateTicket(
        [FromBody] SupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var companySlug = request.CompanySlug.Trim().ToLowerInvariant();
        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Slug == companySlug, cancellationToken);
        if (company == null)
        {
            return BadRequest(new Message("Company slug not found."));
        }

        var ticketId = Guid.NewGuid();
        var status = "Open";
        var payload = JsonSerializer.Serialize(new
        {
            subject = request.Subject.Trim(),
            details = request.Details.Trim(),
            status
        });

        dbContext.AuditLogs.Add(new App.Domain.Entities.AuditLog
        {
            CompanyId = company.Id,
            ActorUserId = User.UserId(),
            EntityName = "SupportTicket",
            EntityId = ticketId,
            Action = "Created",
            ChangedAtUtc = DateTime.UtcNow,
            ChangesJson = payload
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new SupportTicketResponse
        {
            TicketId = ticketId,
            CompanyId = company.Id,
            CompanySlug = company.Slug,
            Subject = request.Subject.Trim(),
            Details = request.Details.Trim(),
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };

        return Created(string.Empty, response);
    }

    private static (string subject, string details, string status) ParseTicketPayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ("-", "-", "Open");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var subject = root.TryGetProperty("subject", out var subjectNode) ? subjectNode.GetString() : "-";
            var details = root.TryGetProperty("details", out var detailsNode) ? detailsNode.GetString() : "-";
            var status = root.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : "Open";
            return (subject ?? "-", details ?? "-", status ?? "Open");
        }
        catch
        {
            return ("-", "-", "Open");
        }
    }
}
