using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.Invoices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class InvoicesController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(entity => entity.DueDateUtc)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{invoiceId:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetById([FromRoute] string companySlug, [FromRoute] Guid invoiceId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);
        if (invoice == null)
        {
            return NotFound(new Message("Invoice not found."));
        }

        return Ok(ToResponse(invoice));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateInvoiceRequest request,
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

        if (request.CostEstimateId.HasValue)
        {
            var estimateExists = await dbContext.CostEstimates
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.CostEstimateId.Value, cancellationToken);
            if (!estimateExists)
            {
                return BadRequest(new Message("Cost estimate does not exist in tenant."));
            }
        }

        var invoiceNumber = request.InvoiceNumber.Trim();
        var duplicateNumber = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(entity => entity.InvoiceNumber == invoiceNumber, cancellationToken);
        if (duplicateNumber)
        {
            return BadRequest(new Message("Invoice number already exists."));
        }

        var invoice = new Invoice
        {
            PatientId = request.PatientId,
            CostEstimateId = request.CostEstimateId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = request.TotalAmount,
            BalanceAmount = request.BalanceAmount,
            DueDateUtc = request.DueDateUtc,
            Status = request.BalanceAmount <= 0 ? InvoiceStatus.Paid : InvoiceStatus.Issued
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, ToResponse(invoice));
    }

    [HttpPut("{invoiceId:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid invoiceId,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (!Enum.TryParse<InvoiceStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid invoice status value."));
        }

        var invoice = await dbContext.Invoices
            .SingleOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);
        if (invoice == null)
        {
            return NotFound(new Message("Invoice not found."));
        }

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            return BadRequest(new Message("Patient does not exist in tenant."));
        }

        if (request.CostEstimateId.HasValue)
        {
            var estimateExists = await dbContext.CostEstimates
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.CostEstimateId.Value, cancellationToken);
            if (!estimateExists)
            {
                return BadRequest(new Message("Cost estimate does not exist in tenant."));
            }
        }

        var invoiceNumber = request.InvoiceNumber.Trim();
        var duplicateNumber = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(entity => entity.InvoiceNumber == invoiceNumber && entity.Id != invoiceId, cancellationToken);
        if (duplicateNumber)
        {
            return BadRequest(new Message("Invoice number already exists."));
        }

        invoice.PatientId = request.PatientId;
        invoice.CostEstimateId = request.CostEstimateId;
        invoice.InvoiceNumber = invoiceNumber;
        invoice.TotalAmount = request.TotalAmount;
        invoice.BalanceAmount = request.BalanceAmount;
        invoice.DueDateUtc = request.DueDateUtc;
        invoice.Status = status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(invoice));
    }

    [HttpDelete("{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid invoiceId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await dbContext.Invoices
            .SingleOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);
        if (invoice == null)
        {
            return NotFound(new Message("Invoice not found."));
        }

        dbContext.Invoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static InvoiceResponse ToResponse(Invoice entity)
    {
        return new InvoiceResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            CostEstimateId = entity.CostEstimateId,
            InvoiceNumber = entity.InvoiceNumber,
            TotalAmount = entity.TotalAmount,
            BalanceAmount = entity.BalanceAmount,
            DueDateUtc = entity.DueDateUtc,
            Status = entity.Status.ToString()
        };
    }
}
