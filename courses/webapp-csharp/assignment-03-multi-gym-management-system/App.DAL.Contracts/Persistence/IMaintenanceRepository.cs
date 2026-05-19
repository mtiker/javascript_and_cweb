using App.Domain.Entities;
using App.Domain.Enums;

namespace App.DAL.Contracts.Persistence;

public interface IMaintenanceRepository
{
    Task<IReadOnlyList<EquipmentModel>> ListEquipmentModelsByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<EquipmentModel?> FindEquipmentModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddEquipmentModelAsync(EquipmentModel entity, CancellationToken cancellationToken = default);
    void RemoveEquipmentModel(EquipmentModel entity);

    Task<IReadOnlyList<Equipment>> ListEquipmentByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> ListEquipmentByGymFilteredAsync(
        Guid gymId,
        EquipmentStatus? status,
        Guid? equipmentModelId,
        string? search,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> ListEquipmentWithModelByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> ListEquipmentDueCandidatesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<Equipment?> FindEquipmentAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<Equipment?> FindEquipmentWithModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddEquipmentAsync(Equipment entity, CancellationToken cancellationToken = default);
    void RemoveEquipment(Equipment entity);

    Task<IReadOnlyList<MaintenanceTask>> ListMaintenanceTasksByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTask>> ListMaintenanceTasksByGymFilteredAsync(
        Guid gymId,
        MaintenanceTaskStatus? status,
        MaintenancePriority? priority,
        MaintenanceTaskType? taskType,
        Guid? equipmentId,
        Guid? assignedStaffId,
        DateTime? dueBeforeUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTask>> ListIncompleteMaintenanceTasksWithStaffByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTask>> ListAssignedTasksWithEquipmentByStaffAsync(Guid gymId, Guid staffId, int limit, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindMaintenanceTaskAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindMaintenanceTaskAggregateAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindLatestCompletedScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<bool> MaintenanceTaskExistsAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddMaintenanceTaskAsync(MaintenanceTask entity, CancellationToken cancellationToken = default);
    void RemoveMaintenanceTask(MaintenanceTask entity);

    Task<GymSettings?> FindGymSettingsAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUserGymRole>> ListGymUsersAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<AppUserGymRole?> FindGymUserRoleAsync(Guid gymId, Guid appUserId, string roleName, CancellationToken cancellationToken = default);
    Task AddGymUserRoleAsync(AppUserGymRole entity, CancellationToken cancellationToken = default);
    void RemoveGymUserRole(AppUserGymRole entity);
    Task<string?> FindUserEmailAsync(Guid appUserId, CancellationToken cancellationToken = default);
}
