using System.Globalization;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;

namespace App.BLL.Services;

public class MaintenanceWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService,
    ISubscriptionTierLimitService subscriptionTierLimitService) : IMaintenanceWorkflowService
{
    public async Task<IReadOnlyCollection<OpeningHoursResponse>> GetOpeningHoursAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        return await dbContext.OpeningHours
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Weekday)
            .Select(entity => new OpeningHoursResponse
            {
                Id = entity.Id,
                Weekday = entity.Weekday,
                OpensAt = entity.OpensAt,
                ClosesAt = entity.ClosesAt
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<OpeningHoursResponse> CreateOpeningHoursAsync(string gymCode, OpeningHoursUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = new OpeningHours
        {
            GymId = gymId,
            Weekday = request.Weekday,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt
        };
        dbContext.OpeningHours.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToOpeningHoursResponse(entity);
    }

    public async Task<OpeningHoursResponse> UpdateOpeningHoursAsync(string gymCode, Guid id, OpeningHoursUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHours.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Opening hours row was not found.");
        entity.Weekday = request.Weekday;
        entity.OpensAt = request.OpensAt;
        entity.ClosesAt = request.ClosesAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToOpeningHoursResponse(entity);
    }

    public async Task DeleteOpeningHoursAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHours.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Opening hours row was not found.");
        dbContext.OpeningHours.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> GetOpeningHourExceptionsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        return await dbContext.OpeningHoursExceptions
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ExceptionDate)
            .Select(entity => new OpeningHoursExceptionResponse
            {
                Id = entity.Id,
                ExceptionDate = entity.ExceptionDate,
                IsClosed = entity.IsClosed,
                OpensAt = entity.OpensAt,
                ClosesAt = entity.ClosesAt,
                Reason = Translate(entity.Reason)
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<OpeningHoursExceptionResponse> CreateOpeningHourExceptionAsync(string gymCode, OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = new OpeningHoursException
        {
            GymId = gymId,
            ExceptionDate = request.ExceptionDate,
            IsClosed = request.IsClosed,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : ToLangStr(request.Reason)
        };
        dbContext.OpeningHoursExceptions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToOpeningHoursExceptionResponse(entity);
    }

    public async Task<OpeningHoursExceptionResponse> UpdateOpeningHourExceptionAsync(string gymCode, Guid id, OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHoursExceptions.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Opening hours exception was not found.");
        entity.ExceptionDate = request.ExceptionDate;
        entity.IsClosed = request.IsClosed;
        entity.OpensAt = request.OpensAt;
        entity.ClosesAt = request.ClosesAt;
        entity.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : ToLangStr(request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToOpeningHoursExceptionResponse(entity);
    }

    public async Task DeleteOpeningHourExceptionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHoursExceptions.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Opening hours exception was not found.");
        dbContext.OpeningHoursExceptions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        return await dbContext.EquipmentModels
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ValidFrom)
            .Select(entity => new EquipmentModelResponse
            {
                Id = entity.Id,
                Name = Translate(entity.Name) ?? string.Empty,
                Type = entity.Type,
                Manufacturer = entity.Manufacturer,
                MaintenanceIntervalDays = entity.MaintenanceIntervalDays,
                Description = Translate(entity.Description)
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = new EquipmentModel
        {
            GymId = gymId,
            Name = ToLangStr(request.Name),
            Type = request.Type,
            Manufacturer = request.Manufacturer?.Trim(),
            MaintenanceIntervalDays = request.MaintenanceIntervalDays,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };
        dbContext.EquipmentModels.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToEquipmentModelResponse(entity);
    }

    public async Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.EquipmentModels.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Equipment model was not found.");
        entity.Name = ToLangStr(request.Name);
        entity.Type = request.Type;
        entity.Manufacturer = request.Manufacturer?.Trim();
        entity.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToEquipmentModelResponse(entity);
    }

    public async Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.EquipmentModels.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Equipment model was not found.");
        dbContext.EquipmentModels.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        return await dbContext.Equipment
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.AssetTag)
            .Select(entity => new EquipmentResponse
            {
                Id = entity.Id,
                EquipmentModelId = entity.EquipmentModelId,
                AssetTag = entity.AssetTag,
                SerialNumber = entity.SerialNumber,
                CurrentStatus = entity.CurrentStatus,
                CommissionedAt = entity.CommissionedAt,
                DecommissionedAt = entity.DecommissionedAt,
                Notes = entity.Notes
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        await subscriptionTierLimitService.EnsureCanCreateEquipmentAsync(gymId, cancellationToken);
        var entity = new Equipment
        {
            GymId = gymId,
            EquipmentModelId = request.EquipmentModelId,
            AssetTag = request.AssetTag?.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            CurrentStatus = request.CurrentStatus,
            CommissionedAt = request.CommissionedAt,
            DecommissionedAt = request.DecommissionedAt,
            Notes = request.Notes?.Trim()
        };
        dbContext.Equipment.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToEquipmentResponse(entity);
    }

    public async Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.Equipment.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Equipment item was not found.");
        entity.EquipmentModelId = request.EquipmentModelId;
        entity.AssetTag = request.AssetTag?.Trim();
        entity.SerialNumber = request.SerialNumber?.Trim();
        entity.CurrentStatus = request.CurrentStatus;
        entity.CommissionedAt = request.CommissionedAt;
        entity.DecommissionedAt = request.DecommissionedAt;
        entity.Notes = request.Notes?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToEquipmentResponse(entity);
    }

    public async Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.Equipment.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Equipment item was not found.");
        dbContext.Equipment.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var tasks = await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedByStaff)
                    .ThenInclude(entity => entity!.Person)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks.Select(ToMaintenanceResponse).ToArray();
    }

    public async Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var equipment = await dbContext.Equipment
            .Include(item => item.EquipmentModel)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == request.EquipmentId)
            ?? throw new NotFoundException("Equipment item was not found.");

        Staff? assignedStaff = null;
        if (request.AssignedStaffId.HasValue)
        {
            assignedStaff = await dbContext.Staff
                               .Include(entity => entity.Person)
                               .FirstOrDefaultAsync(entity => entity.Id == request.AssignedStaffId.Value)
                               ?? throw new NotFoundException("Assigned staff member was not found.");
            if (assignedStaff.GymId != gymId)
            {
                throw new ValidationAppException("Assigned staff member must belong to the active gym.");
            }
        }

        var task = new MaintenanceTask
        {
            GymId = gymId,
            EquipmentId = equipment.Id,
            Equipment = equipment,
            AssignedStaffId = request.AssignedStaffId,
            AssignedStaff = assignedStaff,
            CreatedByStaffId = request.CreatedByStaffId,
            TaskType = request.TaskType,
            Priority = request.Priority,
            Status = request.Status,
            DueAtUtc = request.DueAtUtc,
            Notes = request.Notes
        };

        if (task.TaskType == MaintenanceTaskType.Breakdown && task.Status == MaintenanceTaskStatus.InProgress)
        {
            task.StartedAtUtc = DateTime.UtcNow;
            task.DowntimeStartedAtUtc = DateTime.UtcNow;
            equipment.CurrentStatus = EquipmentStatus.Maintenance;
        }

        dbContext.MaintenanceTasks.Add(task);
        dbContext.MaintenanceTaskAssignmentHistory.Add(new MaintenanceTaskAssignmentHistory
        {
            GymId = gymId,
            MaintenanceTask = task,
            AssignedStaffId = task.AssignedStaffId,
            AssignedByStaffId = request.CreatedByStaffId,
            Notes = "Initial assignment"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId && entity.Id == task.Id)
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedByStaff)
                    .ThenInclude(entity => entity!.Person)
            .FirstAsync(cancellationToken);

        return ToMaintenanceResponse(saved);
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var task = await dbContext.MaintenanceTasks
                   .Include(entity => entity.Equipment)
                       .ThenInclude(entity => entity!.EquipmentModel)
                   .Include(entity => entity.AssignedStaff)
                       .ThenInclude(entity => entity!.Person)
                   .Include(entity => entity.AssignmentHistory)
                       .ThenInclude(entity => entity.AssignedStaff)
                           .ThenInclude(entity => entity!.Person)
                   .Include(entity => entity.AssignmentHistory)
                       .ThenInclude(entity => entity.AssignedByStaff)
                           .ThenInclude(entity => entity!.Person)
                   .FirstOrDefaultAsync(entity => entity.Id == taskId)
                   ?? throw new NotFoundException("Maintenance task was not found.");

        await authorizationService.EnsureMaintenanceTaskAccessAsync(task);

        task.Status = request.Status;
        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? task.Notes : request.Notes.Trim();

        if (request.Status == MaintenanceTaskStatus.InProgress && !task.StartedAtUtc.HasValue)
        {
            task.StartedAtUtc = DateTime.UtcNow;
            if (task.TaskType == MaintenanceTaskType.Breakdown)
            {
                task.DowntimeStartedAtUtc ??= DateTime.UtcNow;
                if (task.Equipment != null && task.Equipment.CurrentStatus == EquipmentStatus.Active)
                {
                    task.Equipment.CurrentStatus = EquipmentStatus.Maintenance;
                }
            }
        }

        if (request.Status == MaintenanceTaskStatus.Done)
        {
            var completedAt = DateTime.UtcNow;
            task.CompletedAtUtc = completedAt;
            task.CompletionNotes = string.IsNullOrWhiteSpace(request.CompletionNotes)
                ? task.CompletionNotes
                : request.CompletionNotes.Trim();

            if (task.TaskType == MaintenanceTaskType.Breakdown)
            {
                task.DowntimeStartedAtUtc ??= task.StartedAtUtc ?? completedAt;
                task.DowntimeEndedAtUtc = completedAt;
                if (task.Equipment != null && task.Equipment.CurrentStatus == EquipmentStatus.Maintenance)
                {
                    task.Equipment.CurrentStatus = EquipmentStatus.Active;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMaintenanceResponse(task);
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var task = await dbContext.MaintenanceTasks
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedByStaff)
                    .ThenInclude(entity => entity!.Person)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == taskId, cancellationToken)
            ?? throw new NotFoundException("Maintenance task was not found.");

        Staff? assignedStaff = null;
        if (request.AssignedStaffId.HasValue)
        {
            assignedStaff = await dbContext.Staff
                .Include(entity => entity.Person)
                .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == request.AssignedStaffId.Value, cancellationToken)
                ?? throw new ValidationAppException("Assigned staff member was not found in the active gym.");
        }

        if (request.AssignedByStaffId.HasValue)
        {
            var assignedByExists = await dbContext.Staff.AnyAsync(entity => entity.GymId == gymId && entity.Id == request.AssignedByStaffId.Value, cancellationToken);
            if (!assignedByExists)
            {
                throw new ValidationAppException("Assignment actor staff member was not found in the active gym.");
            }
        }

        task.AssignedStaffId = request.AssignedStaffId;
        task.AssignedStaff = assignedStaff;

        dbContext.MaintenanceTaskAssignmentHistory.Add(new MaintenanceTaskAssignmentHistory
        {
            GymId = gymId,
            MaintenanceTaskId = task.Id,
            AssignedStaffId = request.AssignedStaffId,
            AssignedByStaffId = request.AssignedByStaffId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? "Assignment updated" : request.Notes.Trim()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId && entity.Id == task.Id)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedByStaff)
                    .ThenInclude(entity => entity!.Person)
            .FirstAsync(cancellationToken);

        return ToMaintenanceResponse(saved);
    }

    public async Task<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>> GetTaskAssignmentHistoryAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var exists = await dbContext.MaintenanceTasks.AnyAsync(entity => entity.GymId == gymId && entity.Id == taskId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Maintenance task was not found.");
        }

        return await dbContext.MaintenanceTaskAssignmentHistory
            .Where(entity => entity.GymId == gymId && entity.MaintenanceTaskId == taskId)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignedByStaff)
                .ThenInclude(entity => entity!.Person)
            .OrderByDescending(entity => entity.AssignedAtUtc)
            .Select(entity => new MaintenanceTaskAssignmentHistoryResponse
            {
                Id = entity.Id,
                MaintenanceTaskId = entity.MaintenanceTaskId,
                AssignedStaffId = entity.AssignedStaffId,
                AssignedStaffName = entity.AssignedStaff == null
                    ? null
                    : $"{entity.AssignedStaff.Person!.FirstName} {entity.AssignedStaff.Person.LastName}".Trim(),
                AssignedByStaffId = entity.AssignedByStaffId,
                AssignedByStaffName = entity.AssignedByStaff == null
                    ? null
                    : $"{entity.AssignedByStaff.Person!.FirstName} {entity.AssignedByStaff.Person.LastName}".Trim(),
                AssignedAtUtc = entity.AssignedAtUtc,
                Notes = entity.Notes
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var equipmentItems = await dbContext.Equipment
            .Include(item => item.EquipmentModel)
            .Where(item => item.GymId == gymId && item.CurrentStatus != EquipmentStatus.Decommissioned)
            .ToListAsync(cancellationToken);

        var createdCount = 0;

        foreach (var equipment in equipmentItems)
        {
            if (equipment.EquipmentModel == null || equipment.EquipmentModel.MaintenanceIntervalDays <= 0)
            {
                continue;
            }

            var latestCompleted = await dbContext.MaintenanceTasks
                .Where(task =>
                    task.GymId == gymId &&
                    task.EquipmentId == equipment.Id &&
                    task.TaskType == MaintenanceTaskType.Scheduled &&
                    task.Status == MaintenanceTaskStatus.Done &&
                    task.CompletedAtUtc.HasValue)
                .OrderByDescending(task => task.CompletedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var hasOpenScheduledTask = await dbContext.MaintenanceTasks.AnyAsync(task =>
                task.GymId == gymId &&
                task.EquipmentId == equipment.Id &&
                task.TaskType == MaintenanceTaskType.Scheduled &&
                task.Status != MaintenanceTaskStatus.Done,
                cancellationToken);

            if (hasOpenScheduledTask)
            {
                continue;
            }

            var baseDate = latestCompleted?.CompletedAtUtc?.Date
                           ?? equipment.CommissionedAt?.ToDateTime(TimeOnly.MinValue).Date
                           ?? DateTime.UtcNow.Date;
            var dueDate = baseDate.AddDays(equipment.EquipmentModel.MaintenanceIntervalDays);

            if (dueDate > DateTime.UtcNow.Date)
            {
                continue;
            }

            dbContext.MaintenanceTasks.Add(new MaintenanceTask
            {
                GymId = gymId,
                EquipmentId = equipment.Id,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.Medium,
                Status = MaintenanceTaskStatus.Open,
                DueAtUtc = dueDate.AddHours(12),
                Notes = "Auto-generated scheduled maintenance task."
            });

            createdCount++;
        }

        if (createdCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return createdCount;
    }

    public async Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.MaintenanceTasks.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new NotFoundException("Maintenance task was not found.");
        dbContext.MaintenanceTasks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var entity = await dbContext.GymSettings.FirstOrDefaultAsync(value => value.GymId == gymId)
                     ?? throw new NotFoundException("Gym settings were not found.");
        return ToGymSettingsResponse(entity);
    }

    public async Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.GymSettings.FirstOrDefaultAsync(value => value.GymId == gymId)
                     ?? throw new NotFoundException("Gym settings were not found.");
        entity.CurrencyCode = request.CurrencyCode.Trim();
        entity.TimeZone = request.TimeZone.Trim();
        entity.AllowNonMemberBookings = request.AllowNonMemberBookings;
        entity.BookingCancellationHours = request.BookingCancellationHours;
        entity.PublicDescription = string.IsNullOrWhiteSpace(request.PublicDescription) ? entity.PublicDescription : ToLangStr(request.PublicDescription);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToGymSettingsResponse(entity);
    }

    public async Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        return await dbContext.AppUserGymRoles
            .Include(entity => entity.AppUser)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.RoleName)
            .Select(entity => new GymUserResponse
            {
                AppUserId = entity.AppUserId,
                Email = entity.AppUser!.Email ?? string.Empty,
                RoleName = entity.RoleName,
                IsActive = entity.IsActive
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var existingLink = await dbContext.AppUserGymRoles.FirstOrDefaultAsync(entity =>
            entity.GymId == gymId &&
            entity.AppUserId == request.AppUserId &&
            entity.RoleName == request.RoleName);
        var isNew = existingLink == null;
        var link = existingLink ?? new AppUserGymRole
        {
            GymId = gymId,
            AppUserId = request.AppUserId,
            RoleName = request.RoleName
        };

        link.IsActive = request.IsActive;

        if (isNew)
        {
            dbContext.AppUserGymRoles.Add(link);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(entity => entity.Id == link.AppUserId)
                   ?? throw new NotFoundException("App user was not found.");

        return new GymUserResponse
        {
            AppUserId = link.AppUserId,
            Email = user.Email ?? string.Empty,
            RoleName = link.RoleName,
            IsActive = link.IsActive
        };
    }

    public async Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.AppUserGymRoles.FirstOrDefaultAsync(value =>
                         value.GymId == gymId &&
                         value.AppUserId == appUserId &&
                         value.RoleName == roleName)
                     ?? throw new NotFoundException("Gym user role was not found.");
        dbContext.AppUserGymRoles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static OpeningHoursResponse ToOpeningHoursResponse(OpeningHours entity)
    {
        return new OpeningHoursResponse
        {
            Id = entity.Id,
            Weekday = entity.Weekday,
            OpensAt = entity.OpensAt,
            ClosesAt = entity.ClosesAt
        };
    }

    private static OpeningHoursExceptionResponse ToOpeningHoursExceptionResponse(OpeningHoursException entity)
    {
        return new OpeningHoursExceptionResponse
        {
            Id = entity.Id,
            ExceptionDate = entity.ExceptionDate,
            IsClosed = entity.IsClosed,
            OpensAt = entity.OpensAt,
            ClosesAt = entity.ClosesAt,
            Reason = Translate(entity.Reason)
        };
    }

    private static EquipmentModelResponse ToEquipmentModelResponse(EquipmentModel entity)
    {
        return new EquipmentModelResponse
        {
            Id = entity.Id,
            Name = Translate(entity.Name) ?? string.Empty,
            Type = entity.Type,
            Manufacturer = entity.Manufacturer,
            MaintenanceIntervalDays = entity.MaintenanceIntervalDays,
            Description = Translate(entity.Description)
        };
    }

    private static EquipmentResponse ToEquipmentResponse(Equipment entity)
    {
        return new EquipmentResponse
        {
            Id = entity.Id,
            EquipmentModelId = entity.EquipmentModelId,
            AssetTag = entity.AssetTag,
            SerialNumber = entity.SerialNumber,
            CurrentStatus = entity.CurrentStatus,
            CommissionedAt = entity.CommissionedAt,
            DecommissionedAt = entity.DecommissionedAt,
            Notes = entity.Notes
        };
    }

    private static GymSettingsResponse ToGymSettingsResponse(GymSettings entity)
    {
        return new GymSettingsResponse
        {
            GymId = entity.GymId,
            CurrencyCode = entity.CurrencyCode,
            TimeZone = entity.TimeZone,
            AllowNonMemberBookings = entity.AllowNonMemberBookings,
            BookingCancellationHours = entity.BookingCancellationHours,
            PublicDescription = Translate(entity.PublicDescription)
        };
    }

    private static MaintenanceTaskResponse ToMaintenanceResponse(MaintenanceTask task)
    {
        var assignmentHistory = task.AssignmentHistory
            .OrderByDescending(entity => entity.AssignedAtUtc)
            .Select(entity => new MaintenanceTaskAssignmentHistoryResponse
            {
                Id = entity.Id,
                MaintenanceTaskId = entity.MaintenanceTaskId,
                AssignedStaffId = entity.AssignedStaffId,
                AssignedStaffName = entity.AssignedStaff == null
                    ? null
                    : $"{entity.AssignedStaff.Person?.FirstName} {entity.AssignedStaff.Person?.LastName}".Trim(),
                AssignedByStaffId = entity.AssignedByStaffId,
                AssignedByStaffName = entity.AssignedByStaff == null
                    ? null
                    : $"{entity.AssignedByStaff.Person?.FirstName} {entity.AssignedByStaff.Person?.LastName}".Trim(),
                AssignedAtUtc = entity.AssignedAtUtc,
                Notes = entity.Notes
            })
            .ToArray();

        return new MaintenanceTaskResponse
        {
            Id = task.Id,
            EquipmentId = task.EquipmentId,
            EquipmentAssetTag = task.Equipment?.AssetTag,
            EquipmentName = Translate(task.Equipment?.EquipmentModel?.Name) ?? task.Equipment?.AssetTag ?? "Equipment",
            AssignedStaffId = task.AssignedStaffId,
            AssignedStaffName = task.AssignedStaff == null
                ? null
                : $"{task.AssignedStaff.Person?.FirstName} {task.AssignedStaff.Person?.LastName}".Trim(),
            CreatedByStaffId = task.CreatedByStaffId,
            TaskType = task.TaskType,
            Priority = task.Priority,
            Status = task.Status,
            DueAtUtc = task.DueAtUtc,
            StartedAtUtc = task.StartedAtUtc,
            CompletedAtUtc = task.CompletedAtUtc,
            DowntimeStartedAtUtc = task.DowntimeStartedAtUtc,
            DowntimeEndedAtUtc = task.DowntimeEndedAtUtc,
            IsOverdue = task.Status != MaintenanceTaskStatus.Done && task.DueAtUtc.HasValue && task.DueAtUtc.Value < DateTime.UtcNow,
            Notes = task.Notes,
            CompletionNotes = task.CompletionNotes,
            AssignmentHistory = assignmentHistory
        };
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
