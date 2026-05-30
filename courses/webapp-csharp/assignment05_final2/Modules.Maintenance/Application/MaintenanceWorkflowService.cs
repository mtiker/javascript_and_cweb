using System.Globalization;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using SharedKernel;
using Base.Domain;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.EquipmentModels;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Modules.Maintenance.Application.Mappers;
using Modules.Maintenance.Application.Persistence;
using Shared.Contracts.ModuleApis;

namespace Modules.Maintenance.Application;

public class MaintenanceWorkflowService(
    IMaintenancePersistenceContext persistenceContext,
    IMaintenanceRepository maintenanceRepository,
    IAuthorizationService authorizationService,
    ISubscriptionTierLimitService subscriptionTierLimitService,
    ITrainingModuleApi trainingModuleApi,
    IMaintenanceMapper mapper) : IMaintenanceWorkflowService
{
    public async Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var models = await maintenanceRepository.ListEquipmentModelsByGymAsync(gymId, cancellationToken);
        return mapper.ToEquipmentModelList(models);
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
        await maintenanceRepository.AddEquipmentModelAsync(entity, cancellationToken);
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipmentModel(entity);
    }

    public async Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await maintenanceRepository.FindEquipmentModelAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment model was not found.");
        entity.Name = ToLangStr(request.Name);
        entity.Type = request.Type;
        entity.Manufacturer = request.Manufacturer?.Trim();
        entity.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipmentModel(entity);
    }

    public async Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await maintenanceRepository.FindEquipmentModelAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment model was not found.");
        maintenanceRepository.RemoveEquipmentModel(entity);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, EquipmentFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var hasFilter = filter is not null && (filter.Status.HasValue || filter.EquipmentModelId.HasValue || !string.IsNullOrWhiteSpace(filter.Search));
        var equipment = hasFilter
            ? await maintenanceRepository.ListEquipmentByGymFilteredAsync(gymId, filter!.Status, filter.EquipmentModelId, filter.Search, cancellationToken)
            : await maintenanceRepository.ListEquipmentByGymAsync(gymId, cancellationToken);
        return mapper.ToEquipmentList(equipment);
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
        await maintenanceRepository.AddEquipmentAsync(entity, cancellationToken);
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipment(entity);
    }

    public async Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await maintenanceRepository.FindEquipmentAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment item was not found.");
        entity.EquipmentModelId = request.EquipmentModelId;
        entity.AssetTag = request.AssetTag?.Trim();
        entity.SerialNumber = request.SerialNumber?.Trim();
        entity.CurrentStatus = request.CurrentStatus;
        entity.CommissionedAt = request.CommissionedAt;
        entity.DecommissionedAt = request.DecommissionedAt;
        entity.Notes = request.Notes?.Trim();
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipment(entity);
    }

    public async Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await maintenanceRepository.FindEquipmentAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment item was not found.");
        maintenanceRepository.RemoveEquipment(entity);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EquipmentResponse> UpdateEquipmentStatusAsync(string gymCode, Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var entity = await maintenanceRepository.FindEquipmentAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment item was not found.");
        entity.CurrentStatus = request.Status;
        if (request.Status == EquipmentStatus.Decommissioned && !entity.DecommissionedAt.HasValue)
        {
            entity.DecommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }
        else if (request.Status == EquipmentStatus.Active)
        {
            entity.DecommissionedAt = null;
        }
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipment(entity);
    }

    public async Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, MaintenanceTaskFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var hasFilter = filter is not null && (filter.Status.HasValue || filter.Priority.HasValue || filter.TaskType.HasValue || filter.EquipmentId.HasValue || filter.AssignedStaffId.HasValue || filter.DueBeforeUtc.HasValue);
        var tasks = hasFilter
            ? await maintenanceRepository.ListMaintenanceTasksByGymFilteredAsync(gymId, filter!.Status, filter.Priority, filter.TaskType, filter.EquipmentId, filter.AssignedStaffId, filter.DueBeforeUtc, cancellationToken)
            : await maintenanceRepository.ListMaintenanceTasksByGymAsync(gymId, cancellationToken);
        return mapper.ToMaintenanceTaskList(tasks, await ResolveStaffNamesAsync(gymId, tasks, cancellationToken));
    }

    public async Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var equipment = await maintenanceRepository.FindEquipmentWithModelAsync(gymId, request.EquipmentId, cancellationToken)
                        ?? throw new NotFoundException("Equipment item was not found.");

        if (request.AssignedStaffId.HasValue)
        {
            await EnsureStaffInGymAsync(gymId, request.AssignedStaffId.Value, "Assigned staff member was not found.", cancellationToken);
        }

        if (request.CreatedByStaffId.HasValue)
        {
            await EnsureStaffInGymAsync(gymId, request.CreatedByStaffId.Value, "Task creator staff member was not found.", cancellationToken);
        }

        var now = DateTime.UtcNow;
        var task = new MaintenanceTask
        {
            GymId = gymId,
            EquipmentId = equipment.Id,
            Equipment = equipment,
            AssignedStaffId = request.AssignedStaffId,
            CreatedByStaffId = request.CreatedByStaffId,
            TaskType = request.TaskType,
            Priority = request.Priority,
            Status = request.Status,
            DueAtUtc = request.DueAtUtc,
            Notes = request.Notes
        };

        if (task.TaskType == MaintenanceTaskType.Breakdown && task.Status == MaintenanceTaskStatus.InProgress)
        {
            task.StartedAtUtc = now;
            task.DowntimeStartedAtUtc = now;
            equipment.CurrentStatus = EquipmentStatus.Maintenance;
        }

        await maintenanceRepository.AddMaintenanceTaskAsync(task, cancellationToken);
        await persistenceContext.SaveChangesAsync(cancellationToken);

        var saved = await maintenanceRepository.FindMaintenanceTaskAggregateAsync(gymId, task.Id, cancellationToken)
                    ?? throw new NotFoundException("Maintenance task was not found after creation.");

        return mapper.ToMaintenanceTask(saved, await ResolveStaffNameAsync(gymId, saved.AssignedStaffId, cancellationToken));
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var task = await maintenanceRepository.FindMaintenanceTaskAggregateAsync(gymId, taskId, cancellationToken)
                   ?? throw new NotFoundException("Maintenance task was not found.");

        await authorizationService.EnsureMaintenanceTaskAccessAsync(task, cancellationToken);

        ApplyTaskStatusUpdate(task, request);

        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToMaintenanceTask(task, await ResolveStaffNameAsync(gymId, task.AssignedStaffId, cancellationToken));
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var task = await maintenanceRepository.FindMaintenanceTaskAggregateAsync(gymId, taskId, cancellationToken)
                   ?? throw new NotFoundException("Maintenance task was not found.");

        if (request.AssignedStaffId.HasValue)
        {
            await EnsureStaffInGymAsync(gymId, request.AssignedStaffId.Value, "Assigned staff member was not found in the active gym.", cancellationToken);
        }

        if (request.AssignedByStaffId.HasValue)
        {
            await EnsureStaffInGymAsync(gymId, request.AssignedByStaffId.Value, "Assignment actor staff member was not found in the active gym.", cancellationToken);
        }

        task.AssignedStaffId = request.AssignedStaffId;
        task.AssignedStaff = null;
        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? task.Notes : request.Notes.Trim();

        await persistenceContext.SaveChangesAsync(cancellationToken);

        var saved = await maintenanceRepository.FindMaintenanceTaskAggregateAsync(gymId, task.Id, cancellationToken)
                    ?? throw new NotFoundException("Maintenance task was not found after assignment update.");

        return mapper.ToMaintenanceTask(saved, await ResolveStaffNameAsync(gymId, saved.AssignedStaffId, cancellationToken));
    }

    public async Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var equipmentItems = await maintenanceRepository.ListEquipmentDueCandidatesAsync(gymId, cancellationToken);
        var createdCount = 0;
        var today = DateTime.UtcNow.Date;

        foreach (var equipment in equipmentItems)
        {
            if (equipment.EquipmentModel == null || equipment.EquipmentModel.MaintenanceIntervalDays <= 0)
            {
                continue;
            }

            if (await maintenanceRepository.HasOpenScheduledTaskAsync(gymId, equipment.Id, cancellationToken))
            {
                continue;
            }

            var latestCompleted = await maintenanceRepository.FindLatestCompletedScheduledTaskAsync(gymId, equipment.Id, cancellationToken);
            var baseDate = latestCompleted?.CompletedAtUtc?.Date
                           ?? equipment.CommissionedAt?.ToDateTime(TimeOnly.MinValue).Date
                           ?? today;
            var dueDate = baseDate.AddDays(equipment.EquipmentModel.MaintenanceIntervalDays);

            if (dueDate > today)
            {
                continue;
            }

            await maintenanceRepository.AddMaintenanceTaskAsync(new MaintenanceTask
            {
                GymId = gymId,
                EquipmentId = equipment.Id,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.Medium,
                Status = MaintenanceTaskStatus.Open,
                DueAtUtc = dueDate.AddHours(12),
                Notes = "Auto-generated scheduled maintenance task."
            }, cancellationToken);

            createdCount++;
        }

        if (createdCount > 0)
        {
            await persistenceContext.SaveChangesAsync(cancellationToken);
        }

        return createdCount;
    }

    public async Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await maintenanceRepository.FindMaintenanceTaskAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Maintenance task was not found.");
        maintenanceRepository.RemoveMaintenanceTask(entity);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureStaffInGymAsync(Guid gymId, Guid staffId, string missingMessage, CancellationToken cancellationToken)
    {
        var staff = await trainingModuleApi.GetStaffSummaryAsync(gymId, staffId, cancellationToken);
        if (staff is null)
        {
            throw new ValidationAppException(missingMessage);
        }
    }

    private async Task<string?> ResolveStaffNameAsync(Guid gymId, Guid? staffId, CancellationToken cancellationToken)
    {
        if (!staffId.HasValue) return null;
        var staff = await trainingModuleApi.GetStaffSummaryAsync(gymId, staffId.Value, cancellationToken);
        return staff?.FullName;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveStaffNamesAsync(
        Guid gymId,
        IEnumerable<MaintenanceTask> tasks,
        CancellationToken cancellationToken)
    {
        var staffIds = tasks
            .Where(task => task.AssignedStaffId.HasValue)
            .Select(task => task.AssignedStaffId!.Value)
            .Distinct()
            .ToArray();
        var dict = new Dictionary<Guid, string>();
        foreach (var staffId in staffIds)
        {
            var staff = await trainingModuleApi.GetStaffSummaryAsync(gymId, staffId, cancellationToken);
            if (staff is not null) dict[staffId] = staff.FullName;
        }
        return dict;
    }

    private static void ApplyTaskStatusUpdate(MaintenanceTask task, MaintenanceStatusUpdateRequest request)
    {
        var now = DateTime.UtcNow;

        task.Status = request.Status;
        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? task.Notes : request.Notes.Trim();

        if (request.Status == MaintenanceTaskStatus.InProgress && !task.StartedAtUtc.HasValue)
        {
            task.StartedAtUtc = now;
            if (task.TaskType == MaintenanceTaskType.Breakdown)
            {
                task.DowntimeStartedAtUtc ??= now;
                if (task.Equipment != null && task.Equipment.CurrentStatus == EquipmentStatus.Active)
                {
                    task.Equipment.CurrentStatus = EquipmentStatus.Maintenance;
                }
            }
        }

        if (request.Status == MaintenanceTaskStatus.Done)
        {
            task.CompletedAtUtc = now;
            task.CompletionNotes = string.IsNullOrWhiteSpace(request.CompletionNotes)
                ? task.CompletionNotes
                : request.CompletionNotes.Trim();

            if (task.TaskType == MaintenanceTaskType.Breakdown)
            {
                task.DowntimeStartedAtUtc ??= task.StartedAtUtc ?? now;
                task.DowntimeEndedAtUtc = now;
                if (task.Equipment != null && task.Equipment.CurrentStatus == EquipmentStatus.Maintenance)
                {
                    task.Equipment.CurrentStatus = EquipmentStatus.Active;
                }
            }
        }
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }
}
