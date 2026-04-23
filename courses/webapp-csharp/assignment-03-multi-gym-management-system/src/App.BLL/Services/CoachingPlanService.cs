using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.CoachingPlans;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CoachingPlanService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService,
    IUserContextService userContextService) : ICoachingPlanService
{
    public async Task<IReadOnlyCollection<CoachingPlanResponse>> GetPlansAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Trainer,
            RoleNames.Member);

        var query = dbContext.CoachingPlans
            .Where(entity => entity.GymId == gymId)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.TrainerStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Items)
                .ThenInclude(entity => entity.DecisionByStaff)
                    .ThenInclude(entity => entity!.Person)
            .AsQueryable();

        if (memberId.HasValue)
        {
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId.Value, cancellationToken);
            query = query.Where(entity => entity.MemberId == memberId.Value);
        }

        var current = userContextService.GetCurrent();
        if (current.HasRole(RoleNames.Member))
        {
            var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                                ?? throw new ForbiddenException("Member profile was not found in the active gym.");
            query = query.Where(entity => entity.MemberId == currentMember.Id);
        }

        if (current.HasRole(RoleNames.Trainer) && !current.HasRole(RoleNames.GymOwner) && !current.HasRole(RoleNames.GymAdmin))
        {
            var currentStaff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken)
                               ?? throw new ForbiddenException("Trainer profile was not found in the active gym.");
            query = query.Where(entity => entity.TrainerStaffId == currentStaff.Id || entity.CreatedByStaffId == currentStaff.Id);
        }

        var plans = await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return plans.Select(ToResponse).ToArray();
    }

    public async Task<CoachingPlanResponse> GetPlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Trainer,
            RoleNames.Member);

        var plan = await LoadPlanAsync(gymId, id, cancellationToken)
                   ?? throw new NotFoundException("Coaching plan was not found.");

        var current = userContextService.GetCurrent();
        if (current.HasRole(RoleNames.Member))
        {
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, plan.MemberId, cancellationToken);
        }

        if (current.HasRole(RoleNames.Trainer) && !current.HasRole(RoleNames.GymOwner) && !current.HasRole(RoleNames.GymAdmin))
        {
            var currentStaff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken)
                               ?? throw new ForbiddenException("Trainer profile was not found in the active gym.");
            if (plan.TrainerStaffId != currentStaff.Id && plan.CreatedByStaffId != currentStaff.Id)
            {
                throw new ForbiddenException("Trainer can access only assigned coaching plans.");
            }
        }

        return ToResponse(plan);
    }

    public async Task<CoachingPlanResponse> CreatePlanAsync(string gymCode, CoachingPlanCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);
        ValidateRequest(request.Title, request.Items);

        if (!await dbContext.Members.AnyAsync(entity => entity.GymId == gymId && entity.Id == request.MemberId, cancellationToken))
        {
            throw new ValidationAppException("Member was not found in the active gym.");
        }

        if (request.TrainerStaffId.HasValue)
        {
            await EnsureStaffBelongsToGymAsync(gymId, request.TrainerStaffId.Value, cancellationToken, "Assigned trainer was not found in the active gym.");
        }

        if (request.CreatedByStaffId.HasValue)
        {
            await EnsureStaffBelongsToGymAsync(gymId, request.CreatedByStaffId.Value, cancellationToken, "Creator staff member was not found in the active gym.");
        }

        var plan = new CoachingPlan
        {
            GymId = gymId,
            MemberId = request.MemberId,
            TrainerStaffId = request.TrainerStaffId,
            CreatedByStaffId = request.CreatedByStaffId,
            Title = request.Title.Trim(),
            Notes = request.Notes?.Trim(),
            Status = CoachingPlanStatus.Draft,
            Items = ParseItems(gymId, request.Items)
        };

        dbContext.CoachingPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(gymId, plan.Id, cancellationToken)
                    ?? throw new NotFoundException("Coaching plan was not found after creation.");

        return ToResponse(saved);
    }

    public async Task<CoachingPlanResponse> UpdatePlanAsync(string gymCode, Guid id, CoachingPlanUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);
        var plan = await dbContext.CoachingPlans
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coaching plan was not found.");

        if (plan.Status is CoachingPlanStatus.Completed or CoachingPlanStatus.Cancelled)
        {
            throw new ValidationAppException("Completed or cancelled coaching plans cannot be edited.");
        }

        ValidateRequest(request.Title, request.Items);

        if (request.TrainerStaffId.HasValue)
        {
            await EnsureStaffBelongsToGymAsync(plan.GymId, request.TrainerStaffId.Value, cancellationToken, "Assigned trainer was not found in the active gym.");
        }

        plan.Title = request.Title.Trim();
        plan.Notes = request.Notes?.Trim();
        plan.TrainerStaffId = request.TrainerStaffId;

        dbContext.CoachingPlanItems.RemoveRange(plan.Items);
        plan.Items = ParseItems(plan.GymId, request.Items);

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(plan.GymId, plan.Id, cancellationToken)
                    ?? throw new NotFoundException("Coaching plan was not found after update.");

        return ToResponse(saved);
    }

    public async Task<CoachingPlanResponse> UpdatePlanStatusAsync(string gymCode, Guid id, CoachingPlanStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);

        var plan = await dbContext.CoachingPlans
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coaching plan was not found.");

        EnsureStatusTransition(plan.Status, request.Status);

        plan.Status = request.Status;
        plan.Notes = string.IsNullOrWhiteSpace(request.Notes) ? plan.Notes : request.Notes.Trim();

        switch (request.Status)
        {
            case CoachingPlanStatus.Published:
                plan.PublishedAtUtc ??= DateTime.UtcNow;
                break;
            case CoachingPlanStatus.Active:
                plan.ActivatedAtUtc ??= DateTime.UtcNow;
                break;
            case CoachingPlanStatus.Completed:
                plan.CompletedAtUtc ??= DateTime.UtcNow;
                foreach (var item in plan.Items.Where(entity => entity.Decision == null))
                {
                    item.Decision = CoachingPlanItemDecision.Completed;
                    item.DecisionAtUtc = DateTime.UtcNow;
                }
                break;
            case CoachingPlanStatus.Cancelled:
                plan.CancelledAtUtc ??= DateTime.UtcNow;
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(plan.GymId, plan.Id, cancellationToken)
                    ?? throw new NotFoundException("Coaching plan was not found after status change.");

        return ToResponse(saved);
    }

    public async Task<CoachingPlanResponse> DecidePlanItemAsync(string gymCode, Guid id, Guid itemId, CoachingPlanItemDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Trainer,
            RoleNames.Member);

        var plan = await dbContext.CoachingPlans
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coaching plan was not found.");

        var item = plan.Items.FirstOrDefault(entity => entity.Id == itemId)
                   ?? throw new NotFoundException("Coaching plan item was not found.");

        var current = userContextService.GetCurrent();
        if (current.HasRole(RoleNames.Member))
        {
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, plan.MemberId, cancellationToken);
            if (request.Decision == CoachingPlanItemDecision.Completed)
            {
                throw new ForbiddenException("Members cannot mark coaching items as completed.");
            }
        }

        var currentStaff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken);

        item.Decision = request.Decision;
        item.DecisionAtUtc = DateTime.UtcNow;
        item.DecisionNotes = request.Notes?.Trim();
        item.DecisionByStaffId = currentStaff?.Id;

        if (plan.Status == CoachingPlanStatus.Published && request.Decision == CoachingPlanItemDecision.Accepted)
        {
            plan.Status = CoachingPlanStatus.Active;
            plan.ActivatedAtUtc ??= DateTime.UtcNow;
        }

        if (plan.Items.All(entity => entity.Decision == CoachingPlanItemDecision.Completed))
        {
            plan.Status = CoachingPlanStatus.Completed;
            plan.CompletedAtUtc ??= DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadPlanAsync(gymId, plan.Id, cancellationToken)
                    ?? throw new NotFoundException("Coaching plan was not found after decision update.");

        return ToResponse(saved);
    }

    public async Task DeletePlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);

        var plan = await dbContext.CoachingPlans
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coaching plan was not found.");

        dbContext.CoachingPlanItems.RemoveRange(plan.Items);
        dbContext.CoachingPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRequest(string? title, IReadOnlyCollection<CoachingPlanItemRequest>? items)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationAppException("Plan title is required.");
        }

        if (items == null || items.Count == 0)
        {
            throw new ValidationAppException("At least one coaching plan item is required.");
        }

        if (items.GroupBy(entity => entity.Sequence).Any(group => group.Count() > 1))
        {
            throw new ValidationAppException("Coaching plan item sequence values must be unique.");
        }
    }

    private static List<CoachingPlanItem> ParseItems(Guid gymId, IEnumerable<CoachingPlanItemRequest> items)
    {
        return items
            .OrderBy(entity => entity.Sequence)
            .Select(entity => new CoachingPlanItem
            {
                GymId = gymId,
                Sequence = entity.Sequence,
                Title = entity.Title.Trim(),
                Notes = entity.Notes?.Trim(),
                TargetDate = entity.TargetDate
            })
            .ToList();
    }

    private async Task EnsureStaffBelongsToGymAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken, string message)
    {
        if (!await dbContext.Staff.AnyAsync(entity => entity.GymId == gymId && entity.Id == staffId, cancellationToken))
        {
            throw new ValidationAppException(message);
        }
    }

    private async Task<CoachingPlan?> LoadPlanAsync(Guid gymId, Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.CoachingPlans
            .Where(entity => entity.GymId == gymId && entity.Id == id)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.TrainerStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Items.OrderBy(item => item.Sequence))
                .ThenInclude(entity => entity.DecisionByStaff)
                    .ThenInclude(entity => entity!.Person)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void EnsureStatusTransition(CoachingPlanStatus current, CoachingPlanStatus next)
    {
        if (current == next)
        {
            return;
        }

        var allowed = current switch
        {
            CoachingPlanStatus.Draft => next is CoachingPlanStatus.Published or CoachingPlanStatus.Cancelled,
            CoachingPlanStatus.Published => next is CoachingPlanStatus.Active or CoachingPlanStatus.Cancelled,
            CoachingPlanStatus.Active => next is CoachingPlanStatus.Completed or CoachingPlanStatus.Cancelled,
            CoachingPlanStatus.Completed => false,
            CoachingPlanStatus.Cancelled => false,
            _ => false
        };

        if (!allowed)
        {
            throw new ValidationAppException($"Invalid coaching plan transition from {current} to {next}.");
        }
    }

    private static CoachingPlanResponse ToResponse(CoachingPlan entity)
    {
        return new CoachingPlanResponse
        {
            Id = entity.Id,
            MemberId = entity.MemberId,
            MemberName = $"{entity.Member?.Person?.FirstName} {entity.Member?.Person?.LastName}".Trim(),
            TrainerStaffId = entity.TrainerStaffId,
            TrainerStaffName = entity.TrainerStaff == null
                ? null
                : $"{entity.TrainerStaff.Person?.FirstName} {entity.TrainerStaff.Person?.LastName}".Trim(),
            CreatedByStaffId = entity.CreatedByStaffId,
            Title = entity.Title,
            Notes = entity.Notes,
            Status = entity.Status,
            PublishedAtUtc = entity.PublishedAtUtc,
            ActivatedAtUtc = entity.ActivatedAtUtc,
            CompletedAtUtc = entity.CompletedAtUtc,
            CancelledAtUtc = entity.CancelledAtUtc,
            Items = entity.Items
                .OrderBy(item => item.Sequence)
                .Select(item => new CoachingPlanItemResponse
                {
                    Id = item.Id,
                    Sequence = item.Sequence,
                    Title = item.Title,
                    Notes = item.Notes,
                    TargetDate = item.TargetDate,
                    Decision = item.Decision,
                    DecisionAtUtc = item.DecisionAtUtc,
                    DecisionByStaffName = item.DecisionByStaff == null
                        ? null
                        : $"{item.DecisionByStaff.Person?.FirstName} {item.DecisionByStaff.Person?.LastName}".Trim(),
                    DecisionNotes = item.DecisionNotes
                })
                .ToArray()
        };
    }
}
