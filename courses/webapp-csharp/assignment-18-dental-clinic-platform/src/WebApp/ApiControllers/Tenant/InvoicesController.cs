using App.BLL.Contracts.Finance;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.Invoices;
using App.DTO.v1.PaymentPlans;
using App.DTO.v1.Payments;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class InvoicesController(IInvoiceService invoiceService, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoices = await invoiceService.ListAsync(User.UserId(), patientId, cancellationToken);
        return Ok(invoices.Select(ToSummaryResponse).ToList());
    }

    [HttpGet("{invoiceId:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDetailResponse>> GetById(
        [FromRoute] string companySlug,
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await invoiceService.GetAsync(User.UserId(), invoiceId, cancellationToken);
        return Ok(ToDetailResponse(invoice));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDetailResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await invoiceService.CreateAsync(
            User.UserId(),
            new CreateInvoiceCommand(
                request.PatientId,
                request.CostEstimateId,
                request.InvoiceNumber,
                request.DueDateUtc,
                request.Lines.Select(ToCommand).ToArray()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { version = "1", companySlug, invoiceId = invoice.Id }, ToDetailResponse(invoice));
    }

    [HttpPost("generate-from-procedures")]
    [ProducesResponseType(typeof(InvoiceDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDetailResponse>> GenerateFromProcedures(
        [FromRoute] string companySlug,
        [FromBody] GenerateInvoiceFromProceduresRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await invoiceService.GenerateFromProceduresAsync(
            User.UserId(),
            new GenerateInvoiceFromProceduresCommand(
                request.PatientId,
                request.CostEstimateId,
                request.InvoiceNumber,
                request.DueDateUtc,
                request.TreatmentIds.ToArray()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { version = "1", companySlug, invoiceId = invoice.Id }, ToDetailResponse(invoice));
    }

    [HttpPost("{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> AddPayment(
        [FromRoute] string companySlug,
        [FromRoute] Guid invoiceId,
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var payment = await invoiceService.AddPaymentAsync(
            User.UserId(),
            invoiceId,
            new CreatePaymentCommand(
                request.Amount,
                request.PaidAtUtc,
                request.Method,
                request.Reference,
                request.Notes),
            cancellationToken);

        return Created(string.Empty, ToPaymentResponse(payment));
    }

    [HttpPut("{invoiceId:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDetailResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid invoiceId,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var invoice = await invoiceService.UpdateAsync(
            User.UserId(),
            new UpdateInvoiceCommand(
                invoiceId,
                request.PatientId,
                request.CostEstimateId,
                request.InvoiceNumber,
                request.DueDateUtc,
                request.Lines.Select(ToCommand).ToArray()),
            cancellationToken);

        return Ok(ToDetailResponse(invoice));
    }

    [HttpDelete("{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid invoiceId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        await invoiceService.DeleteAsync(User.UserId(), invoiceId, cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static InvoiceLineCommand ToCommand(InvoiceLineRequest entity)
    {
        return new InvoiceLineCommand(
            entity.TreatmentId,
            entity.PlanItemId,
            entity.Description,
            entity.Quantity,
            entity.UnitPrice,
            entity.CoverageAmount);
    }

    private static InvoiceResponse ToSummaryResponse(InvoiceSummaryResult entity)
    {
        return new InvoiceResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            CostEstimateId = entity.CostEstimateId,
            InvoiceNumber = entity.InvoiceNumber,
            TotalAmount = entity.TotalAmount,
            CoverageAmount = entity.CoverageAmount,
            PatientResponsibilityAmount = entity.PatientResponsibilityAmount,
            PaidAmount = entity.PaidAmount,
            BalanceAmount = entity.BalanceAmount,
            DueDateUtc = entity.DueDateUtc,
            Status = entity.Status
        };
    }

    private static InvoiceDetailResponse ToDetailResponse(InvoiceDetailResult entity)
    {
        return new InvoiceDetailResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            CostEstimateId = entity.CostEstimateId,
            InvoiceNumber = entity.InvoiceNumber,
            TotalAmount = entity.TotalAmount,
            CoverageAmount = entity.CoverageAmount,
            PatientResponsibilityAmount = entity.PatientResponsibilityAmount,
            PaidAmount = entity.PaidAmount,
            BalanceAmount = entity.BalanceAmount,
            DueDateUtc = entity.DueDateUtc,
            Status = entity.Status,
            Lines = entity.Lines.Select(line => new InvoiceLineResponse
            {
                Id = line.Id,
                TreatmentId = line.TreatmentId,
                PlanItemId = line.PlanItemId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal,
                CoverageAmount = line.CoverageAmount,
                PatientAmount = line.PatientAmount
            }).ToArray(),
            Payments = entity.Payments.Select(ToPaymentResponse).ToArray(),
            PaymentPlan = entity.PaymentPlan == null
                ? null
                : new PaymentPlanResponse
                {
                    Id = entity.PaymentPlan.Id,
                    InvoiceId = entity.PaymentPlan.InvoiceId,
                    StartsAtUtc = entity.PaymentPlan.StartsAtUtc,
                    Status = entity.PaymentPlan.Status,
                    Terms = entity.PaymentPlan.Terms,
                    ScheduledAmount = entity.PaymentPlan.ScheduledAmount,
                    RemainingAmount = entity.PaymentPlan.RemainingAmount,
                    Installments = entity.PaymentPlan.Installments.Select(installment => new PaymentPlanInstallmentResponse
                    {
                        Id = installment.Id,
                        DueDateUtc = installment.DueDateUtc,
                        Amount = installment.Amount,
                        Status = installment.Status,
                        PaidAtUtc = installment.PaidAtUtc
                    }).ToArray()
                }
        };
    }

    private static PaymentResponse ToPaymentResponse(PaymentResult entity)
    {
        return new PaymentResponse
        {
            Id = entity.Id,
            InvoiceId = entity.InvoiceId,
            Amount = entity.Amount,
            PaidAtUtc = entity.PaidAtUtc,
            Method = entity.Method,
            Reference = entity.Reference,
            Notes = entity.Notes
        };
    }
}
