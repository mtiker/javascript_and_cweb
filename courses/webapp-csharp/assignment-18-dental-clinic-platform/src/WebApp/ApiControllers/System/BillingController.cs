using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.System.Billing;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/billing")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.SystemAdmin + "," + RoleNames.SystemBilling)]
public class BillingController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SubscriptionSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SubscriptionSummaryResponse>>> Subscriptions(CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Companies.IgnoreQueryFilters().AsNoTracking(),
                subscription => subscription.CompanyId,
                company => company.Id,
                (subscription, company) => new SubscriptionSummaryResponse
                {
                    SubscriptionId = subscription.Id,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    CompanySlug = company.Slug,
                    Tier = subscription.Tier.ToString(),
                    Status = subscription.Status.ToString(),
                    UserLimit = subscription.UserLimit,
                    EntityLimit = subscription.EntityLimit,
                    StartsAtUtc = subscription.StartsAtUtc,
                    EndsAtUtc = subscription.EndsAtUtc
                })
            .OrderBy(item => item.CompanyName)
            .ToListAsync(cancellationToken);

        return Ok(subscriptions);
    }

    [HttpPut("subscriptions/{subscriptionId:guid}")]
    [ProducesResponseType(typeof(SubscriptionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionSummaryResponse>> UpdateSubscription(
        [FromRoute] Guid subscriptionId,
        [FromBody] UpdateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SubscriptionTier>(request.Tier, true, out var tier))
        {
            return BadRequest(new Message("Invalid subscription tier."));
        }

        if (!Enum.TryParse<SubscriptionStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid subscription status."));
        }

        var subscription = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.Id == subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return NotFound(new Message("Subscription not found."));
        }

        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == subscription.CompanyId, cancellationToken);
        if (company == null)
        {
            return NotFound(new Message("Company not found for subscription."));
        }

        subscription.Tier = tier;
        subscription.Status = status;
        subscription.UserLimit = request.UserLimit;
        subscription.EntityLimit = request.EntityLimit;
        if (status == SubscriptionStatus.Cancelled)
        {
            subscription.EndsAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new SubscriptionSummaryResponse
        {
            SubscriptionId = subscription.Id,
            CompanyId = company.Id,
            CompanyName = company.Name,
            CompanySlug = company.Slug,
            Tier = subscription.Tier.ToString(),
            Status = subscription.Status.ToString(),
            UserLimit = subscription.UserLimit,
            EntityLimit = subscription.EntityLimit,
            StartsAtUtc = subscription.StartsAtUtc,
            EndsAtUtc = subscription.EndsAtUtc
        });
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvoiceSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceSummaryResponse>>> Invoices(CancellationToken cancellationToken)
    {
        var invoices = await dbContext.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Companies.IgnoreQueryFilters().AsNoTracking(),
                invoice => invoice.CompanyId,
                company => company.Id,
                (invoice, company) => new InvoiceSummaryResponse
                {
                    InvoiceId = invoice.Id,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    CompanySlug = company.Slug,
                    InvoiceNumber = invoice.InvoiceNumber,
                    TotalAmount = invoice.TotalAmount,
                    BalanceAmount = invoice.BalanceAmount,
                    Status = invoice.Status.ToString(),
                    DueDateUtc = invoice.DueDateUtc
                })
            .OrderBy(item => item.DueDateUtc)
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpPut("invoices/{invoiceId:guid}/status")]
    [ProducesResponseType(typeof(InvoiceSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceSummaryResponse>> UpdateInvoiceStatus(
        [FromRoute] Guid invoiceId,
        [FromBody] UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<InvoiceStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid invoice status."));
        }

        var invoice = await dbContext.Invoices
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);
        if (invoice == null)
        {
            return NotFound(new Message("Invoice not found."));
        }

        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == invoice.CompanyId, cancellationToken);
        if (company == null)
        {
            return NotFound(new Message("Company not found for invoice."));
        }

        invoice.Status = status;
        if (status == InvoiceStatus.Paid)
        {
            invoice.BalanceAmount = 0m;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new InvoiceSummaryResponse
        {
            InvoiceId = invoice.Id,
            CompanyId = company.Id,
            CompanyName = company.Name,
            CompanySlug = company.Slug,
            InvoiceNumber = invoice.InvoiceNumber,
            TotalAmount = invoice.TotalAmount,
            BalanceAmount = invoice.BalanceAmount,
            Status = invoice.Status.ToString(),
            DueDateUtc = invoice.DueDateUtc
        });
    }
}
