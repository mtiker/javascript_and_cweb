using App.BLL.Contracts;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.TreatmentPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager + "," + RoleNames.CompanyEmployee)]
public class TreatmentPlansController(
    ITreatmentPlanService treatmentPlanService,
    ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TreatmentPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TreatmentPlanResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plans = await treatmentPlanService.ListAsync(User.UserId(), patientId, cancellationToken);
        return Ok(plans.Select(ToResponse).ToList());
    }

    [HttpGet("{planId:guid}")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanResponse>> GetById([FromRoute] string companySlug, [FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await treatmentPlanService.GetAsync(User.UserId(), planId, cancellationToken);
        return Ok(ToResponse(plan));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<ActionResult<TreatmentPlanResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await treatmentPlanService.CreateAsync(
            User.UserId(),
            new CreateTreatmentPlanCommand(
                request.PatientId,
                request.DentistId,
                request.Items.Select(ToCommand).ToArray()),
            cancellationToken);

        return Created(string.Empty, ToResponse(plan));
    }

    [HttpPut("{planId:guid}")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<ActionResult<TreatmentPlanResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid planId,
        [FromBody] UpdateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await treatmentPlanService.UpdateAsync(
            User.UserId(),
            new UpdateTreatmentPlanCommand(
                planId,
                request.PatientId,
                request.DentistId,
                request.Items?.Select(ToCommand).ToArray()),
            cancellationToken);

        return Ok(ToResponse(plan));
    }

    [HttpPost("{planId:guid}/submit")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<ActionResult<TreatmentPlanResponse>> Submit(
        [FromRoute] string companySlug,
        [FromRoute] Guid planId,
        [FromBody] SubmitTreatmentPlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        _ = request;

        await treatmentPlanService.SubmitAsync(User.UserId(), planId, cancellationToken);
        var saved = await treatmentPlanService.GetAsync(User.UserId(), planId, cancellationToken);

        return Ok(ToResponse(saved));
    }

    [HttpDelete("{planId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        await treatmentPlanService.DeleteAsync(User.UserId(), planId, cancellationToken);
        return NoContent();
    }

    [HttpGet("openitems")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OpenPlanItemResponse>), StatusCodes.Status200OK)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<ActionResult<IReadOnlyCollection<OpenPlanItemResponse>>> OpenItems(
        [FromRoute] string companySlug,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var pendingItems = await treatmentPlanService.ListOpenItemsAsync(User.UserId(), cancellationToken);
        return Ok(pendingItems.Select(item => new OpenPlanItemResponse
        {
            PlanId = item.PlanId,
            PlanItemId = item.PlanItemId,
            PatientId = item.PatientId,
            PatientName = item.PatientName,
            TreatmentTypeName = item.TreatmentTypeName,
            Sequence = item.Sequence,
            Urgency = item.Urgency,
            EstimatedPrice = item.EstimatedPrice
        }).ToList());
    }

    [HttpPost("recorditemdecision")]
    [ProducesResponseType(typeof(PlanItemDecisionResponse), StatusCodes.Status200OK)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
    public async Task<ActionResult<PlanItemDecisionResponse>> RecordItemDecision(
        [FromRoute] string companySlug,
        [FromBody] PlanItemDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        if (!Enum.TryParse<PlanItemDecision>(request.Decision, true, out var decision))
        {
            return BadRequest(new Message("Invalid decision value."));
        }

        var result = await treatmentPlanService.RecordPlanItemDecisionAsync(
            User.UserId(),
            new RecordPlanItemDecisionCommand(
                request.PlanId,
                request.PlanItemId,
                decision,
                request.Notes),
            cancellationToken);

        return Ok(new PlanItemDecisionResponse
        {
            PlanId = result.PlanId,
            PlanItemId = result.PlanItemId,
            PlanStatus = result.PlanStatus,
            ItemDecision = result.ItemDecision
        });
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static TreatmentPlanItemCommand ToCommand(TreatmentPlanItemRequest request)
    {
        return new TreatmentPlanItemCommand(
            request.TreatmentTypeId,
            request.Sequence,
            request.Urgency,
            request.EstimatedPrice);
    }

    private static TreatmentPlanResponse ToResponse(TreatmentPlanResult result)
    {
        return new TreatmentPlanResponse
        {
            Id = result.Id,
            PatientId = result.PatientId,
            DentistId = result.DentistId,
            Status = result.Status,
            SubmittedAtUtc = result.SubmittedAtUtc,
            ApprovedAtUtc = result.ApprovedAtUtc,
            IsLocked = result.IsLocked,
            Items = result.Items
                .Select(entity => new TreatmentPlanItemResponse
                {
                    Id = entity.Id,
                    TreatmentTypeId = entity.TreatmentTypeId,
                    TreatmentTypeName = entity.TreatmentTypeName,
                    Sequence = entity.Sequence,
                    Urgency = entity.Urgency,
                    EstimatedPrice = entity.EstimatedPrice,
                    Decision = entity.Decision,
                    DecisionAtUtc = entity.DecisionAtUtc,
                    DecisionNotes = entity.DecisionNotes
                })
                .ToList()
        };
    }
}
