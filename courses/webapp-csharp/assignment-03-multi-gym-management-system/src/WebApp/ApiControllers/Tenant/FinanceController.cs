using App.DTO.v1.Finance;
using Asp.Versioning;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.MembershipFinance.Contracts;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class FinanceController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("finance-workspace/me")]
    [ProducesResponseType(typeof(FinanceWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceWorkspaceResponse>> GetCurrentWorkspace(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GetCurrentFinanceWorkspaceQuery(gymCode), cancellationToken));
    }

    [HttpGet("finance-workspace/members/{memberId:guid}")]
    [ProducesResponseType(typeof(FinanceWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceWorkspaceResponse>> GetMemberWorkspace(string gymCode, Guid memberId, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GetMemberFinanceWorkspaceQuery(gymCode, memberId), cancellationToken));
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceResponse>>> GetInvoices(string gymCode, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new ListInvoicesQuery(gymCode, memberId), cancellationToken));
    }

    [HttpGet("invoices/{id:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new GetInvoiceQuery(gymCode, id), cancellationToken));
    }

    [HttpPost("invoices")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceResponse>> CreateInvoice(string gymCode, [FromBody] InvoiceCreateRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.SendAsync(new CreateInvoiceCommand(gymCode, request), cancellationToken);
        return CreatedAtAction(nameof(GetInvoice), new { version = "1.0", gymCode, id = created.Id }, created);
    }

    [HttpPost("invoices/{id:guid}/payments")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> AddInvoicePayment(string gymCode, Guid id, [FromBody] InvoicePaymentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new PostInvoicePaymentCommand(gymCode, id, request), cancellationToken));
    }

    [HttpPost("invoices/{id:guid}/refunds")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceResponse>> AddInvoiceRefund(string gymCode, Guid id, [FromBody] InvoicePaymentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.SendAsync(new PostInvoiceRefundCommand(gymCode, id, request), cancellationToken));
    }
}
