using App.DAL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfMaintenanceRepository(AppDbContext dbContext) : IMaintenanceRepository
{
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

    public async Task<IReadOnlyList<Equipment>> ListEquipmentWithModelByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.Equipment
            .AsNoTracking()
            .Include(entity => entity.EquipmentModel)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.AssetTag)
            .Take(limit)
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

    public async Task<IReadOnlyList<MaintenanceTask>> ListIncompleteMaintenanceTasksWithStaffByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(entity => entity.Equipment)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Where(entity => entity.GymId == gymId && entity.Status != MaintenanceTaskStatus.Done)
            .OrderBy(entity => entity.DueAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTask>> ListAssignedTasksWithEquipmentByStaffAsync(Guid gymId, Guid staffId, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(entity => entity.Equipment)
                .ThenInclude(entity => entity!.EquipmentModel)
            .Include(entity => entity.AssignedStaff)
                .ThenInclude(entity => entity!.Person)
            .Where(entity => entity.GymId == gymId && entity.AssignedStaffId == staffId)
            .OrderBy(entity => entity.DueAtUtc)
            .Take(limit)
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
                .ThenInclude(entity => entity!.EquipmentModel);
    }
}
