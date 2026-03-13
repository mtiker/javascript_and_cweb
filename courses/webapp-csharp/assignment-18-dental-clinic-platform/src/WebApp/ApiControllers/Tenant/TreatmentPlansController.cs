using App.BLL.Contracts;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.TreatmentPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class TreatmentPlansController(
    ITreatmentPlanService treatmentPlanService,
    AppDbContext dbContext,
    ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TreatmentPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TreatmentPlanResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(plans.Select(ToResponse).ToList());
    }

    [HttpGet("{planId:guid}")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanResponse>> GetById([FromRoute] string companySlug, [FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        return plan == null
            ? NotFound(new Message("Treatment plan not found."))
            : Ok(ToResponse(plan));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentPlanResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        if (request.Items.Count == 0)
        {
            return BadRequest(new Message("At least one plan item is required."));
        }

        if (!await dbContext.Patients.AsNoTracking().AnyAsync(entity => entity.Id == request.PatientId, cancellationToken))
        {
            return BadRequest(new Message("Patient does not exist in tenant."));
        }

        if (request.DentistId.HasValue &&
            !await dbContext.Dentists.AsNoTracking().AnyAsync(entity => entity.Id == request.DentistId.Value, cancellationToken))
        {
            return BadRequest(new Message("Dentist does not exist in tenant."));
        }

        var parseResult = await ParsePlanItemsAsync(request.Items, cancellationToken);
        if (!parseResult.Success)
        {
            return BadRequest(new Message(parseResult.ErrorMessage!));
        }

        var plan = new TreatmentPlan
        {
            PatientId = request.PatientId,
            DentistId = request.DentistId,
            Status = "Pending"
        };

        foreach (var item in parseResult.Items!)
        {
            plan.Items.Add(item);
        }

        dbContext.TreatmentPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .SingleAsync(entity => entity.Id == plan.Id, cancellationToken);

        return Created(string.Empty, ToResponse(saved));
    }

    [HttpPut("{planId:guid}")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentPlanResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid planId,
        [FromBody] UpdateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Treatment plan not found."));
        }

        if (request.PatientId.HasValue)
        {
            var patientExists = await dbContext.Patients
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.PatientId.Value, cancellationToken);
            if (!patientExists)
            {
                return BadRequest(new Message("Patient does not exist in tenant."));
            }

            plan.PatientId = request.PatientId.Value;
        }

        if (request.DentistId.HasValue)
        {
            var dentistExists = await dbContext.Dentists
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.DentistId.Value, cancellationToken);
            if (!dentistExists)
            {
                return BadRequest(new Message("Dentist does not exist in tenant."));
            }

            plan.DentistId = request.DentistId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            if (!IsAllowedPlanStatus(status))
            {
                return BadRequest(new Message("Invalid treatment plan status."));
            }

            plan.Status = status;
            plan.ApprovedAtUtc = status is "Accepted" or "PartiallyAccepted"
                ? DateTime.UtcNow
                : null;
        }

        if (request.Items != null)
        {
            if (request.Items.Count == 0)
            {
                return BadRequest(new Message("At least one plan item is required."));
            }

            var parseResult = await ParsePlanItemsAsync(request.Items, cancellationToken);
            if (!parseResult.Success)
            {
                return BadRequest(new Message(parseResult.ErrorMessage!));
            }

            dbContext.PlanItems.RemoveRange(plan.Items);
            plan.Items.Clear();

            foreach (var item in parseResult.Items!)
            {
                plan.Items.Add(item);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .SingleAsync(entity => entity.Id == plan.Id, cancellationToken);

        return Ok(ToResponse(saved));
    }

    [HttpDelete("{planId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);
        if (plan == null)
        {
            return NotFound(new Message("Treatment plan not found."));
        }

        dbContext.PlanItems.RemoveRange(plan.Items);
        dbContext.TreatmentPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("openitems")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OpenPlanItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OpenPlanItemResponse>>> OpenItems(
        [FromRoute] string companySlug,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var pendingItems = await dbContext.PlanItems
            .AsNoTracking()
            .Where(entity => entity.Decision == PlanItemDecision.Pending)
            .Join(
                dbContext.TreatmentPlans.AsNoTracking(),
                item => item.TreatmentPlanId,
                plan => plan.Id,
                (item, plan) => new { item, plan })
            .Join(
                dbContext.Patients.AsNoTracking(),
                pair => pair.plan.PatientId,
                patient => patient.Id,
                (pair, patient) => new { pair.item, pair.plan, patient })
            .Join(
                dbContext.TreatmentTypes.AsNoTracking(),
                pair => pair.item.TreatmentTypeId,
                treatmentType => treatmentType.Id,
                (pair, treatmentType) => new OpenPlanItemResponse
                {
                    PlanId = pair.plan.Id,
                    PlanItemId = pair.item.Id,
                    PatientId = pair.patient.Id,
                    PatientName = ((pair.patient.FirstName ?? string.Empty) + " " + (pair.patient.LastName ?? string.Empty)).Trim(),
                    TreatmentTypeName = treatmentType.Name,
                    Sequence = pair.item.Sequence,
                    Urgency = pair.item.Urgency.ToString(),
                    EstimatedPrice = pair.item.EstimatedPrice
                })
            .OrderBy(item => item.PatientName)
            .ThenBy(item => item.Sequence)
            .ToListAsync(cancellationToken);

        return Ok(pendingItems);
    }

    [HttpPost("recorditemdecision")]
    [ProducesResponseType(typeof(PlanItemDecisionResponse), StatusCodes.Status200OK)]
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

    private async Task<(bool Success, string? ErrorMessage, List<PlanItem>? Items)> ParsePlanItemsAsync(
        ICollection<TreatmentPlanItemRequest> requestItems,
        CancellationToken cancellationToken)
    {
        var duplicates = requestItems
            .GroupBy(entity => entity.Sequence)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            return (false, "Plan item sequences must be unique.", null);
        }

        var typeIds = requestItems.Select(entity => entity.TreatmentTypeId).Distinct().ToList();
        var existingTypeIds = await dbContext.TreatmentTypes
            .AsNoTracking()
            .Where(entity => typeIds.Contains(entity.Id))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (existingTypeIds.Count != typeIds.Count)
        {
            return (false, "One or more treatment type ids are invalid.", null);
        }

        var items = new List<PlanItem>(requestItems.Count);
        foreach (var requestItem in requestItems.OrderBy(entity => entity.Sequence))
        {
            if (!Enum.TryParse<UrgencyLevel>(requestItem.Urgency, true, out var urgency))
            {
                return (false, "Invalid urgency value in plan items.", null);
            }

            items.Add(new PlanItem
            {
                TreatmentTypeId = requestItem.TreatmentTypeId,
                Sequence = requestItem.Sequence,
                Urgency = urgency,
                EstimatedPrice = requestItem.EstimatedPrice,
                Decision = PlanItemDecision.Pending
            });
        }

        return (true, null, items);
    }

    private static bool IsAllowedPlanStatus(string status)
    {
        return status is "Draft" or "Pending" or "Accepted" or "PartiallyAccepted" or "Deferred";
    }

    private static TreatmentPlanResponse ToResponse(TreatmentPlan plan)
    {
        return new TreatmentPlanResponse
        {
            Id = plan.Id,
            PatientId = plan.PatientId,
            DentistId = plan.DentistId,
            Status = plan.Status,
            ApprovedAtUtc = plan.ApprovedAtUtc,
            Items = plan.Items
                .OrderBy(entity => entity.Sequence)
                .Select(entity => new TreatmentPlanItemResponse
                {
                    Id = entity.Id,
                    TreatmentTypeId = entity.TreatmentTypeId,
                    TreatmentTypeName = entity.TreatmentType?.Name ?? "-",
                    Sequence = entity.Sequence,
                    Urgency = entity.Urgency.ToString(),
                    EstimatedPrice = entity.EstimatedPrice,
                    Decision = entity.Decision.ToString(),
                    DecisionAtUtc = entity.DecisionAtUtc,
                    DecisionNotes = entity.DecisionNotes
                })
                .ToList()
        };
    }
}
