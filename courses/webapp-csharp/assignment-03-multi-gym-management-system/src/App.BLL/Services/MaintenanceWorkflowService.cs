using System.Globalization;
using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Tenant;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class MaintenanceWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMaintenanceWorkflowService
{
    public async Task<IReadOnlyCollection<OpeningHoursResponse>> GetOpeningHoursAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
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
            .ToArrayAsync();
    }

    public async Task<OpeningHoursResponse> CreateOpeningHoursAsync(string gymCode, OpeningHoursUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = new OpeningHours
        {
            GymId = gymId,
            Weekday = request.Weekday,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt
        };
        dbContext.OpeningHours.Add(entity);
        await dbContext.SaveChangesAsync();
        return ToOpeningHoursResponse(entity);
    }

    public async Task<OpeningHoursResponse> UpdateOpeningHoursAsync(string gymCode, Guid id, OpeningHoursUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHours.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Opening hours row was not found.");
        entity.Weekday = request.Weekday;
        entity.OpensAt = request.OpensAt;
        entity.ClosesAt = request.ClosesAt;
        await dbContext.SaveChangesAsync();
        return ToOpeningHoursResponse(entity);
    }

    public async Task DeleteOpeningHoursAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHours.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Opening hours row was not found.");
        dbContext.OpeningHours.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> GetOpeningHourExceptionsAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
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
            .ToArrayAsync();
    }

    public async Task<OpeningHoursExceptionResponse> CreateOpeningHourExceptionAsync(string gymCode, OpeningHoursExceptionUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
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
        await dbContext.SaveChangesAsync();
        return ToOpeningHoursExceptionResponse(entity);
    }

    public async Task<OpeningHoursExceptionResponse> UpdateOpeningHourExceptionAsync(string gymCode, Guid id, OpeningHoursExceptionUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHoursExceptions.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Opening hours exception was not found.");
        entity.ExceptionDate = request.ExceptionDate;
        entity.IsClosed = request.IsClosed;
        entity.OpensAt = request.OpensAt;
        entity.ClosesAt = request.ClosesAt;
        entity.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : ToLangStr(request.Reason);
        await dbContext.SaveChangesAsync();
        return ToOpeningHoursExceptionResponse(entity);
    }

    public async Task DeleteOpeningHourExceptionAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.OpeningHoursExceptions.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Opening hours exception was not found.");
        dbContext.OpeningHoursExceptions.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
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
            .ToArrayAsync();
    }

    public async Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
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
        await dbContext.SaveChangesAsync();
        return ToEquipmentModelResponse(entity);
    }

    public async Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.EquipmentModels.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Equipment model was not found.");
        entity.Name = ToLangStr(request.Name);
        entity.Type = request.Type;
        entity.Manufacturer = request.Manufacturer?.Trim();
        entity.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await dbContext.SaveChangesAsync();
        return ToEquipmentModelResponse(entity);
    }

    public async Task DeleteEquipmentModelAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.EquipmentModels.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Equipment model was not found.");
        dbContext.EquipmentModels.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
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
            .ToArrayAsync();
    }

    public async Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
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
        await dbContext.SaveChangesAsync();
        return ToEquipmentResponse(entity);
    }

    public async Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.Equipment.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Equipment item was not found.");
        entity.EquipmentModelId = request.EquipmentModelId;
        entity.AssetTag = request.AssetTag?.Trim();
        entity.SerialNumber = request.SerialNumber?.Trim();
        entity.CurrentStatus = request.CurrentStatus;
        entity.CommissionedAt = request.CommissionedAt;
        entity.DecommissionedAt = request.DecommissionedAt;
        entity.Notes = request.Notes?.Trim();
        await dbContext.SaveChangesAsync();
        return ToEquipmentResponse(entity);
    }

    public async Task DeleteEquipmentAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.Equipment.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Equipment item was not found.");
        dbContext.Equipment.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);
        return await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Select(entity => new MaintenanceTaskResponse
            {
                Id = entity.Id,
                EquipmentId = entity.EquipmentId,
                AssignedStaffId = entity.AssignedStaffId,
                CreatedByStaffId = entity.CreatedByStaffId,
                TaskType = entity.TaskType,
                Priority = entity.Priority,
                Status = entity.Status,
                DueAtUtc = entity.DueAtUtc,
                StartedAtUtc = entity.StartedAtUtc,
                CompletedAtUtc = entity.CompletedAtUtc,
                Notes = entity.Notes
            })
            .ToArrayAsync();
    }

    public async Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var equipment = await dbContext.Equipment
            .Include(item => item.EquipmentModel)
            .FirstOrDefaultAsync(entity => entity.Id == request.EquipmentId)
            ?? throw new AppNotFoundException("Equipment item was not found.");

        if (request.AssignedStaffId.HasValue)
        {
            var assignedStaff = await dbContext.Staff.FirstOrDefaultAsync(entity => entity.Id == request.AssignedStaffId.Value)
                               ?? throw new AppNotFoundException("Assigned staff member was not found.");
            if (assignedStaff.GymId != gymId)
            {
                throw new AppValidationException("Assigned staff member must belong to the active gym.");
            }
        }

        var task = new MaintenanceTask
        {
            GymId = gymId,
            EquipmentId = equipment.Id,
            AssignedStaffId = request.AssignedStaffId,
            CreatedByStaffId = request.CreatedByStaffId,
            TaskType = request.TaskType,
            Priority = request.Priority,
            Status = request.Status,
            DueAtUtc = request.DueAtUtc,
            Notes = request.Notes
        };

        dbContext.MaintenanceTasks.Add(task);
        await dbContext.SaveChangesAsync();

        return ToMaintenanceResponse(task);
    }

    public async Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Caretaker);

        var task = await dbContext.MaintenanceTasks.FirstOrDefaultAsync(entity => entity.Id == taskId)
                   ?? throw new AppNotFoundException("Maintenance task was not found.");

        await authorizationService.EnsureMaintenanceTaskAccessAsync(task);

        task.Status = request.Status;
        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? task.Notes : request.Notes.Trim();

        if (request.Status == MaintenanceTaskStatus.InProgress && !task.StartedAtUtc.HasValue)
        {
            task.StartedAtUtc = DateTime.UtcNow;
        }

        if (request.Status == MaintenanceTaskStatus.Done)
        {
            task.CompletedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        return ToMaintenanceResponse(task);
    }

    public async Task<int> GenerateDueScheduledTasksAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var equipmentItems = await dbContext.Equipment
            .Include(item => item.EquipmentModel)
            .Where(item => item.GymId == gymId && item.CurrentStatus != EquipmentStatus.Decommissioned)
            .ToListAsync();

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
                .FirstOrDefaultAsync();

            var hasOpenScheduledTask = await dbContext.MaintenanceTasks.AnyAsync(task =>
                task.GymId == gymId &&
                task.EquipmentId == equipment.Id &&
                task.TaskType == MaintenanceTaskType.Scheduled &&
                task.Status != MaintenanceTaskStatus.Done);

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
            await dbContext.SaveChangesAsync();
        }

        return createdCount;
    }

    public async Task DeleteMaintenanceTaskAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.MaintenanceTasks.FirstOrDefaultAsync(value => value.Id == id)
                     ?? throw new AppNotFoundException("Maintenance task was not found.");
        dbContext.MaintenanceTasks.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var entity = await dbContext.GymSettings.FirstOrDefaultAsync(value => value.GymId == gymId)
                     ?? throw new AppNotFoundException("Gym settings were not found.");
        return ToGymSettingsResponse(entity);
    }

    public async Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.GymSettings.FirstOrDefaultAsync(value => value.GymId == gymId)
                     ?? throw new AppNotFoundException("Gym settings were not found.");
        entity.CurrencyCode = request.CurrencyCode.Trim();
        entity.TimeZone = request.TimeZone.Trim();
        entity.AllowNonMemberBookings = request.AllowNonMemberBookings;
        entity.BookingCancellationHours = request.BookingCancellationHours;
        entity.PublicDescription = string.IsNullOrWhiteSpace(request.PublicDescription) ? entity.PublicDescription : ToLangStr(request.PublicDescription);
        await dbContext.SaveChangesAsync();
        return ToGymSettingsResponse(entity);
    }

    public async Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
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
            .ToArrayAsync();
    }

    public async Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
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

        await dbContext.SaveChangesAsync();
        var user = await dbContext.Users.FirstOrDefaultAsync(entity => entity.Id == link.AppUserId)
                   ?? throw new AppNotFoundException("App user was not found.");

        return new GymUserResponse
        {
            AppUserId = link.AppUserId,
            Email = user.Email ?? string.Empty,
            RoleName = link.RoleName,
            IsActive = link.IsActive
        };
    }

    public async Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await dbContext.AppUserGymRoles.FirstOrDefaultAsync(value =>
                         value.GymId == gymId &&
                         value.AppUserId == appUserId &&
                         value.RoleName == roleName)
                     ?? throw new AppNotFoundException("Gym user role was not found.");
        dbContext.AppUserGymRoles.Remove(entity);
        await dbContext.SaveChangesAsync();
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
        return new MaintenanceTaskResponse
        {
            Id = task.Id,
            EquipmentId = task.EquipmentId,
            AssignedStaffId = task.AssignedStaffId,
            CreatedByStaffId = task.CreatedByStaffId,
            TaskType = task.TaskType,
            Priority = task.Priority,
            Status = task.Status,
            DueAtUtc = task.DueAtUtc,
            StartedAtUtc = task.StartedAtUtc,
            CompletedAtUtc = task.CompletedAtUtc,
            Notes = task.Notes
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
