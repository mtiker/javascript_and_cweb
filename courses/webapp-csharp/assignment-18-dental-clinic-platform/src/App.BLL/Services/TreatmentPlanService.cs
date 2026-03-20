using App.BLL.Contracts;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class TreatmentPlanService(AppDbContext dbContext, ITenantAccessService tenantAccessService) : ITreatmentPlanService
{
    public async Task<IReadOnlyCollection<TreatmentPlanResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken)
    {
        await EnsureReadAccessAsync(userId, cancellationToken);

        var query = dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        var plans = await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return plans.Select(ToResult).ToList();
    }

    public async Task<TreatmentPlanResult> GetAsync(Guid userId, Guid planId, CancellationToken cancellationToken)
    {
        await EnsureReadAccessAsync(userId, cancellationToken);

        var plan = await LoadPlanAsync(planId, asNoTracking: true, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        return ToResult(plan);
    }

    public async Task<TreatmentPlanResult> CreateAsync(Guid userId, CreateTreatmentPlanCommand command, CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        if (command.Items.Count == 0)
        {
            throw new ValidationAppException("At least one plan item is required.");
        }

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new ValidationAppException("Patient does not exist in current company.");
        }

        if (command.DentistId.HasValue)
        {
            var dentistExists = await dbContext.Dentists
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == command.DentistId.Value, cancellationToken);
            if (!dentistExists)
            {
                throw new ValidationAppException("Dentist does not exist in current company.");
            }
        }

        var items = await ParsePlanItemsAsync(command.Items, cancellationToken);

        var plan = new TreatmentPlan
        {
            PatientId = command.PatientId,
            DentistId = command.DentistId
        };

        foreach (var item in items)
        {
            plan.Items.Add(item);
        }

        dbContext.TreatmentPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(plan.Id, asNoTracking: true, cancellationToken);
        return ToResult(saved!);
    }

    public async Task<TreatmentPlanResult> UpdateAsync(Guid userId, UpdateTreatmentPlanCommand command, CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        var plan = await LoadPlanAsync(command.PlanId, asNoTracking: false, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        if (command.PatientId.HasValue)
        {
            var patientExists = await dbContext.Patients
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == command.PatientId.Value, cancellationToken);
            if (!patientExists)
            {
                throw new ValidationAppException("Patient does not exist in current company.");
            }

            plan.PatientId = command.PatientId.Value;
        }

        if (command.DentistId.HasValue)
        {
            var dentistExists = await dbContext.Dentists
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == command.DentistId.Value, cancellationToken);
            if (!dentistExists)
            {
                throw new ValidationAppException("Dentist does not exist in current company.");
            }

            plan.DentistId = command.DentistId.Value;
        }

        if (command.Items != null)
        {
            if (TreatmentPlanWorkflow.IsLockedForItemReplacement(plan))
            {
                throw new ValidationAppException("Submitted or decided treatment plan items cannot be replaced.");
            }

            if (command.Items.Count == 0)
            {
                throw new ValidationAppException("At least one plan item is required.");
            }

            var items = await ParsePlanItemsAsync(command.Items, cancellationToken);
            dbContext.PlanItems.RemoveRange(plan.Items);
            plan.Items.Clear();

            foreach (var item in items)
            {
                plan.Items.Add(item);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(plan.Id, asNoTracking: true, cancellationToken);
        return ToResult(saved!);
    }

    public async Task<SubmitTreatmentPlanResult> SubmitAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        if (plan.Items.Count == 0)
        {
            throw new ValidationAppException("Treatment plan must contain at least one item before submission.");
        }

        plan.SubmittedAtUtc ??= DateTime.UtcNow;
        TreatmentPlanWorkflow.ApplyDerivedState(plan);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitTreatmentPlanResult(
            plan.Id,
            plan.Status.ToString(),
            plan.SubmittedAtUtc,
            plan.ApprovedAtUtc);
    }

    public async Task DeleteAsync(Guid userId, Guid planId, CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        dbContext.PlanItems.RemoveRange(plan.Items);
        dbContext.TreatmentPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OpenPlanItemResult>> ListOpenItemsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        var openItems = await dbContext.PlanItems
            .AsNoTracking()
            .Where(entity => entity.Decision == PlanItemDecision.Pending)
            .Join(
                dbContext.TreatmentPlans.AsNoTracking().Where(entity => entity.SubmittedAtUtc != null),
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
                (pair, treatmentType) => new
                {
                    PlanId = pair.plan.Id,
                    PlanItemId = pair.item.Id,
                    PatientId = pair.patient.Id,
                    pair.patient.FirstName,
                    pair.patient.LastName,
                    TreatmentTypeName = treatmentType.Name,
                    pair.item.Sequence,
                    pair.item.Urgency,
                    pair.item.EstimatedPrice
                })
            .OrderBy(item => item.LastName ?? string.Empty)
            .ThenBy(item => item.FirstName ?? string.Empty)
            .ThenBy(item => item.Sequence)
            .ToListAsync(cancellationToken);

        return openItems
            .Select(item => new OpenPlanItemResult(
                item.PlanId,
                item.PlanItemId,
                item.PatientId,
                $"{item.FirstName ?? string.Empty} {item.LastName ?? string.Empty}".Trim(),
                item.TreatmentTypeName,
                item.Sequence,
                item.Urgency.ToString(),
                item.EstimatedPrice))
            .ToList();
    }

    public async Task<PlanDecisionResult> RecordPlanItemDecisionAsync(
        Guid userId,
        RecordPlanItemDecisionCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureManageAccessAsync(userId, cancellationToken);

        var plan = await dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == command.PlanId, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException("Treatment plan was not found.");
        }

        if (plan.SubmittedAtUtc == null)
        {
            throw new ValidationAppException("Treatment plan must be submitted before recording patient decisions.");
        }

        var item = plan.Items.SingleOrDefault(entity => entity.Id == command.PlanItemId);
        if (item == null)
        {
            throw new NotFoundException("Plan item was not found.");
        }

        item.Decision = command.Decision;
        item.DecisionAtUtc = DateTime.UtcNow;
        item.DecisionNotes = command.Notes;

        TreatmentPlanWorkflow.ApplyDerivedState(plan);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PlanDecisionResult(plan.Id, item.Id, plan.Status.ToString(), item.Decision.ToString());
    }

    private Task EnsureReadAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager,
            RoleNames.CompanyEmployee);
    }

    private Task EnsureManageAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager);
    }

    private async Task<TreatmentPlan?> LoadPlanAsync(Guid planId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.TreatmentPlans
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);
    }

    private async Task<List<PlanItem>> ParsePlanItemsAsync(
        IReadOnlyCollection<TreatmentPlanItemCommand> commandItems,
        CancellationToken cancellationToken)
    {
        var duplicates = commandItems
            .GroupBy(entity => entity.Sequence)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ValidationAppException("Plan item sequences must be unique.");
        }

        var typeIds = commandItems.Select(entity => entity.TreatmentTypeId).Distinct().ToList();
        var existingTypeIds = await dbContext.TreatmentTypes
            .AsNoTracking()
            .Where(entity => typeIds.Contains(entity.Id))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (existingTypeIds.Count != typeIds.Count)
        {
            throw new ValidationAppException("One or more treatment type ids are invalid.");
        }

        var items = new List<PlanItem>(commandItems.Count);
        foreach (var commandItem in commandItems.OrderBy(entity => entity.Sequence))
        {
            if (!Enum.TryParse<UrgencyLevel>(commandItem.Urgency, true, out var urgency))
            {
                throw new ValidationAppException("Invalid urgency value in plan items.");
            }

            items.Add(new PlanItem
            {
                TreatmentTypeId = commandItem.TreatmentTypeId,
                Sequence = commandItem.Sequence,
                Urgency = urgency,
                EstimatedPrice = commandItem.EstimatedPrice,
                Decision = PlanItemDecision.Pending
            });
        }

        return items;
    }

    private static TreatmentPlanResult ToResult(TreatmentPlan plan)
    {
        return new TreatmentPlanResult(
            plan.Id,
            plan.PatientId,
            plan.DentistId,
            plan.Status.ToString(),
            plan.SubmittedAtUtc,
            plan.ApprovedAtUtc,
            TreatmentPlanWorkflow.IsLockedForItemReplacement(plan),
            plan.Items
                .OrderBy(entity => entity.Sequence)
                .Select(entity => new TreatmentPlanItemResult(
                    entity.Id,
                    entity.TreatmentTypeId,
                    entity.TreatmentType?.Name ?? "-",
                    entity.Sequence,
                    entity.Urgency.ToString(),
                    entity.EstimatedPrice,
                    entity.Decision.ToString(),
                    entity.DecisionAtUtc,
                    entity.DecisionNotes))
                .ToList());
    }
}
