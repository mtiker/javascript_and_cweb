using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfMaintenanceRepository(AppDbContext dbContext) : IMaintenanceRepository
{
    public async Task<IReadOnlyList<OpeningHours>> ListOpeningHoursByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OpeningHours
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Weekday)
            .ToListAsync(cancellationToken);
    }

    public Task<OpeningHours?> FindOpeningHoursAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OpeningHours.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public async Task AddOpeningHoursAsync(OpeningHours entity, CancellationToken cancellationToken = default)
    {
        await dbContext.OpeningHours.AddAsync(entity, cancellationToken);
    }

    public void RemoveOpeningHours(OpeningHours entity)
    {
        dbContext.OpeningHours.Remove(entity);
    }

    public async Task<IReadOnlyList<OpeningHoursException>> ListOpeningHourExceptionsByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OpeningHoursExceptions
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ExceptionDate)
            .ToListAsync(cancellationToken);
    }

    public Task<OpeningHoursException?> FindOpeningHourExceptionAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OpeningHoursExceptions.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public async Task AddOpeningHourExceptionAsync(OpeningHoursException entity, CancellationToken cancellationToken = default)
    {
        await dbContext.OpeningHoursExceptions.AddAsync(entity, cancellationToken);
    }

    public void RemoveOpeningHourException(OpeningHoursException entity)
    {
        dbContext.OpeningHoursExceptions.Remove(entity);
    }

    public async Task<IReadOnlyList<EquipmentModel>> ListEquipmentModelsByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EquipmentModels
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ValidFrom)
            .ToListAsync(cancellationToken);
    }

    public Task<EquipmentModel?> FindEquipmentModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.EquipmentModels.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public async Task AddEquipmentModelAsync(EquipmentModel entity, CancellationToken cancellationToken = default)
    {
        await dbContext.EquipmentModels.AddAsync(entity, cancellationToken);
    }

    public void RemoveEquipmentModel(EquipmentModel entity)
    {
        dbContext.EquipmentModels.Remove(entity);
    }

    public async Task<IReadOnlyList<Equipment>> ListEquipmentByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Equipment
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.AssetTag)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Equipment>> ListEquipmentDueCandidatesAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Equipment
            .Include(entity => entity.EquipmentModel)
            .Where(entity => entity.GymId == gymId && entity.CurrentStatus != EquipmentStatus.Decommissioned)
            .ToListAsync(cancellationToken);
    }

    public Task<Equipment?> FindEquipmentAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Equipment.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public Task<Equipment?> FindEquipmentWithModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Equipment
            .Include(entity => entity.EquipmentModel)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public async Task AddEquipmentAsync(Equipment entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Equipment.AddAsync(entity, cancellationToken);
    }

    public void RemoveEquipment(Equipment entity)
    {
        dbContext.Equipment.Remove(entity);
    }

    public async Task<IReadOnlyList<MaintenanceTask>> ListMaintenanceTasksByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await MaintenanceTaskAggregateQuery()
            .Where(entity => entity.GymId == gymId)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<MaintenanceTask?> FindMaintenanceTaskAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.MaintenanceTasks.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public Task<MaintenanceTask?> FindMaintenanceTaskAggregateAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return MaintenanceTaskAggregateQuery().FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public Task<MaintenanceTask?> FindLatestCompletedScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return dbContext.MaintenanceTasks
            .Where(task =>
                task.GymId == gymId &&
                task.EquipmentId == equipmentId &&
                task.TaskType == MaintenanceTaskType.Scheduled &&
                task.Status == MaintenanceTaskStatus.Done &&
                task.CompletedAtUtc.HasValue)
            .OrderByDescending(task => task.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasOpenScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return dbContext.MaintenanceTasks.AnyAsync(task =>
                task.GymId == gymId &&
                task.EquipmentId == equipmentId &&
                task.TaskType == MaintenanceTaskType.Scheduled &&
                task.Status != MaintenanceTaskStatus.Done,
            cancellationToken);
    }

    public Task<bool> MaintenanceTaskExistsAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.MaintenanceTasks.AnyAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken);
    }

    public async Task AddMaintenanceTaskAsync(MaintenanceTask entity, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceTasks.AddAsync(entity, cancellationToken);
    }

    public void RemoveMaintenanceTask(MaintenanceTask entity)
    {
        dbContext.MaintenanceTasks.Remove(entity);
    }

    public async Task<IReadOnlyList<MaintenanceTaskAssignmentHistory>> ListAssignmentHistoryAsync(Guid gymId, Guid maintenanceTaskId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MaintenanceTaskAssignmentHistory
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignedByStaff)
                .ThenInclude(entity => entity!.Person)
            .Where(entity => entity.GymId == gymId && entity.MaintenanceTaskId == maintenanceTaskId)
            .OrderByDescending(entity => entity.AssignedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAssignmentHistoryAsync(MaintenanceTaskAssignmentHistory entity, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceTaskAssignmentHistory.AddAsync(entity, cancellationToken);
    }

    public Task<GymSettings?> FindGymSettingsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return dbContext.GymSettings.FirstOrDefaultAsync(entity => entity.GymId == gymId, cancellationToken);
    }

    public async Task<IReadOnlyList<AppUserGymRole>> ListGymUsersAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AppUserGymRoles
            .Include(entity => entity.AppUser)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.RoleName)
            .ToListAsync(cancellationToken);
    }

    public Task<AppUserGymRole?> FindGymUserRoleAsync(Guid gymId, Guid appUserId, string roleName, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUserGymRoles.FirstOrDefaultAsync(
            entity => entity.GymId == gymId && entity.AppUserId == appUserId && entity.RoleName == roleName,
            cancellationToken);
    }

    public async Task AddGymUserRoleAsync(AppUserGymRole entity, CancellationToken cancellationToken = default)
    {
        await dbContext.AppUserGymRoles.AddAsync(entity, cancellationToken);
    }

    public void RemoveGymUserRole(AppUserGymRole entity)
    {
        dbContext.AppUserGymRoles.Remove(entity);
    }

    public async Task<string?> FindUserEmailAsync(Guid appUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Where(entity => entity.Id == appUserId)
            .Select(entity => entity.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<MaintenanceTask> MaintenanceTaskAggregateQuery()
    {
        return dbContext.MaintenanceTasks
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.AssignmentHistory)
                .ThenInclude(entity => entity.AssignedByStaff)
                    .ThenInclude(entity => entity!.Person);
    }
}
