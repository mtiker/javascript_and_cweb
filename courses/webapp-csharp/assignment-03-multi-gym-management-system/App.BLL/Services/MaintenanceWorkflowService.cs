using System.Globalization;
using App.DAL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mappers;
using App.Domain;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;

namespace App.BLL.Services;

public class MaintenanceWorkflowService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    ISubscriptionTierLimitService subscriptionTierLimitService,
    IMaintenanceMapper mapper) : IMaintenanceWorkflowService
{
    public async Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var models = await unitOfWork.Maintenance.ListEquipmentModelsByGymAsync(gymId, cancellationToken);
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
        await unitOfWork.Maintenance.AddEquipmentModelAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipmentModel(entity);
    }

    public async Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindEquipmentModelAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment model was not found.");
        entity.Name = ToLangStr(request.Name);
        entity.Type = request.Type;
        entity.Manufacturer = request.Manufacturer?.Trim();
        entity.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipmentModel(entity);
    }

    public async Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindEquipmentModelAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment model was not found.");
        unitOfWork.Maintenance.RemoveEquipmentModel(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var equipment = await unitOfWork.Maintenance.ListEquipmentByGymAsync(gymId, cancellationToken);
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
        await unitOfWork.Maintenance.AddEquipmentAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipment(entity);
    }

    public async Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindEquipmentAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment item was not found.");
        entity.EquipmentModelId = request.EquipmentModelId;
        entity.AssetTag = request.AssetTag?.Trim();
        entity.SerialNumber = request.SerialNumber?.Trim();
        entity.CurrentStatus = request.CurrentStatus;
        entity.CommissionedAt = request.CommissionedAt;
        entity.DecommissionedAt = request.DecommissionedAt;
        entity.Notes = request.Notes?.Trim();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToEquipment(entity);
    }

    public async Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindEquipmentAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Equipment item was not found.");
        unitOfWork.Maintenance.RemoveEquipment(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var tasks = await unitOfWork.Maintenance.ListMaintenanceTasksByGymAsync(gymId, cancellationToken);
        return mapper.ToMaintenanceTaskList(tasks);
    }

    public async Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var equipment = await unitOfWork.Maintenance.FindEquipmentWithModelAsync(gymId, request.EquipmentId, cancellationToken)
                        ?? throw new NotFoundException("Equipment item was not found.");

        var assignedStaff = request.AssignedStaffId.HasValue
            ? await FindStaffInGymAsync(gymId, request.AssignedStaffId.Value, "Assigned staff member was not found.", cancellationToken)
            : null;

        if (request.CreatedByStaffId.HasValue)
        {
            await FindStaffInGymAsync(gymId, request.CreatedByStaffId.Value, "Task creator staff member was not found.", cancellationToken);
        }

        var now = DateTime.UtcNow;
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
            task.StartedAtUtc = now;
            task.DowntimeStartedAtUtc = now;
            equipment.CurrentStatus = EquipmentStatus.Maintenance;
        }

        await unitOfWork.Maintenance.AddMaintenanceTaskAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.Maintenance.FindMaintenanceTaskAggregateAsync(gymId, task.Id, cancellationToken)
                    ?? throw new NotFoundException("Maintenance task was not found after creation.");

        return mapper.ToMaintenanceTask(saved);
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        var task = await unitOfWork.Maintenance.FindMaintenanceTaskAggregateAsync(gymId, taskId, cancellationToken)
                   ?? throw new NotFoundException("Maintenance task was not found.");

        await authorizationService.EnsureMaintenanceTaskAccessAsync(task, cancellationToken);

        ApplyTaskStatusUpdate(task, request);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToMaintenanceTask(task);
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var task = await unitOfWork.Maintenance.FindMaintenanceTaskAggregateAsync(gymId, taskId, cancellationToken)
                   ?? throw new NotFoundException("Maintenance task was not found.");

        var assignedStaff = request.AssignedStaffId.HasValue
            ? await FindStaffInGymAsync(gymId, request.AssignedStaffId.Value, "Assigned staff member was not found in the active gym.", cancellationToken)
            : null;

        if (request.AssignedByStaffId.HasValue)
        {
            await FindStaffInGymAsync(gymId, request.AssignedByStaffId.Value, "Assignment actor staff member was not found in the active gym.", cancellationToken);
        }

        task.AssignedStaffId = request.AssignedStaffId;
        task.AssignedStaff = assignedStaff;
        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? task.Notes : request.Notes.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.Maintenance.FindMaintenanceTaskAggregateAsync(gymId, task.Id, cancellationToken)
                    ?? throw new NotFoundException("Maintenance task was not found after assignment update.");

        return mapper.ToMaintenanceTask(saved);
    }

    public async Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var equipmentItems = await unitOfWork.Maintenance.ListEquipmentDueCandidatesAsync(gymId, cancellationToken);
        var createdCount = 0;
        var today = DateTime.UtcNow.Date;

        foreach (var equipment in equipmentItems)
        {
            if (equipment.EquipmentModel == null || equipment.EquipmentModel.MaintenanceIntervalDays <= 0)
            {
                continue;
            }

            if (await unitOfWork.Maintenance.HasOpenScheduledTaskAsync(gymId, equipment.Id, cancellationToken))
            {
                continue;
            }

            var latestCompleted = await unitOfWork.Maintenance.FindLatestCompletedScheduledTaskAsync(gymId, equipment.Id, cancellationToken);
            var baseDate = latestCompleted?.CompletedAtUtc?.Date
                           ?? equipment.CommissionedAt?.ToDateTime(TimeOnly.MinValue).Date
                           ?? today;
            var dueDate = baseDate.AddDays(equipment.EquipmentModel.MaintenanceIntervalDays);

            if (dueDate > today)
            {
                continue;
            }

            await unitOfWork.Maintenance.AddMaintenanceTaskAsync(new MaintenanceTask
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
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return createdCount;
    }

    public async Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindMaintenanceTaskAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Maintenance task was not found.");
        unitOfWork.Maintenance.RemoveMaintenanceTask(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var entity = await unitOfWork.Maintenance.FindGymSettingsAsync(gymId, cancellationToken)
                     ?? throw new NotFoundException("Gym settings were not found.");
        return mapper.ToGymSettings(entity);
    }

    public async Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindGymSettingsAsync(gymId, cancellationToken)
                     ?? throw new NotFoundException("Gym settings were not found.");
        entity.CurrencyCode = request.CurrencyCode.Trim();
        entity.TimeZone = request.TimeZone.Trim();
        entity.AllowNonMemberBookings = request.AllowNonMemberBookings;
        entity.BookingCancellationHours = request.BookingCancellationHours;
        entity.PublicDescription = string.IsNullOrWhiteSpace(request.PublicDescription) ? entity.PublicDescription : ToLangStr(request.PublicDescription);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.ToGymSettings(entity);
    }

    public async Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var users = await unitOfWork.Maintenance.ListGymUsersAsync(gymId, cancellationToken);
        return mapper.ToGymUserList(users);
    }

    public async Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var link = await unitOfWork.Maintenance.FindGymUserRoleAsync(gymId, request.AppUserId, request.RoleName, cancellationToken);
        if (link == null)
        {
            link = new AppUserGymRole
            {
                GymId = gymId,
                AppUserId = request.AppUserId,
                RoleName = request.RoleName
            };
            await unitOfWork.Maintenance.AddGymUserRoleAsync(link, cancellationToken);
        }

        link.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        link.AppUser ??= new App.Domain.Identity.AppUser
        {
            Id = link.AppUserId,
            Email = await unitOfWork.Maintenance.FindUserEmailAsync(link.AppUserId, cancellationToken)
                    ?? throw new NotFoundException("App user was not found.")
        };

        return mapper.ToGymUser(link);
    }

    public async Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await unitOfWork.Maintenance.FindGymUserRoleAsync(gymId, appUserId, roleName, cancellationToken)
                     ?? throw new NotFoundException("Gym user role was not found.");
        unitOfWork.Maintenance.RemoveGymUserRole(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Staff> FindStaffInGymAsync(Guid gymId, Guid staffId, string missingMessage, CancellationToken cancellationToken)
    {
        var staff = (await unitOfWork.Repository<Staff>().ListAsync(
                entity => entity.GymId == gymId && entity.Id == staffId,
                cancellationToken))
            .FirstOrDefault();

        return staff ?? throw new ValidationAppException(missingMessage);
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
