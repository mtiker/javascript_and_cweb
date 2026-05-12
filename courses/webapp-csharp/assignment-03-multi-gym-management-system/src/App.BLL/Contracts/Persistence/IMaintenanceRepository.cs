using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IMaintenanceRepository
{
    Task<IReadOnlyList<OpeningHours>> ListOpeningHoursByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<OpeningHours?> FindOpeningHoursAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddOpeningHoursAsync(OpeningHours entity, CancellationToken cancellationToken = default);
    void RemoveOpeningHours(OpeningHours entity);

    Task<IReadOnlyList<OpeningHoursException>> ListOpeningHourExceptionsByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<OpeningHoursException?> FindOpeningHourExceptionAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddOpeningHourExceptionAsync(OpeningHoursException entity, CancellationToken cancellationToken = default);
    void RemoveOpeningHourException(OpeningHoursException entity);

    Task<IReadOnlyList<EquipmentModel>> ListEquipmentModelsByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<EquipmentModel?> FindEquipmentModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddEquipmentModelAsync(EquipmentModel entity, CancellationToken cancellationToken = default);
    void RemoveEquipmentModel(EquipmentModel entity);

    Task<IReadOnlyList<Equipment>> ListEquipmentByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> ListEquipmentWithModelByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> ListEquipmentDueCandidatesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<Equipment?> FindEquipmentAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<Equipment?> FindEquipmentWithModelAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddEquipmentAsync(Equipment entity, CancellationToken cancellationToken = default);
    void RemoveEquipment(Equipment entity);

    Task<IReadOnlyList<MaintenanceTask>> ListMaintenanceTasksByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTask>> ListIncompleteMaintenanceTasksWithStaffByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTask>> ListAssignedTasksWithEquipmentByStaffAsync(Guid gymId, Guid staffId, int limit, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindMaintenanceTaskAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindMaintenanceTaskAggregateAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTask?> FindLatestCompletedScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenScheduledTaskAsync(Guid gymId, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<bool> MaintenanceTaskExistsAsync(Guid gymId, Guid id, CancellationToken cancellationToken = default);
    Task AddMaintenanceTaskAsync(MaintenanceTask entity, CancellationToken cancellationToken = default);
    void RemoveMaintenanceTask(MaintenanceTask entity);

    Task<IReadOnlyList<MaintenanceTaskAssignmentHistory>> ListAssignmentHistoryAsync(Guid gymId, Guid maintenanceTaskId, CancellationToken cancellationToken = default);
    Task AddAssignmentHistoryAsync(MaintenanceTaskAssignmentHistory entity, CancellationToken cancellationToken = default);

    Task<GymSettings?> FindGymSettingsAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUserGymRole>> ListGymUsersAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<AppUserGymRole?> FindGymUserRoleAsync(Guid gymId, Guid appUserId, string roleName, CancellationToken cancellationToken = default);
    Task AddGymUserRoleAsync(AppUserGymRole entity, CancellationToken cancellationToken = default);
    void RemoveGymUserRole(AppUserGymRole entity);
    Task<string?> FindUserEmailAsync(Guid appUserId, CancellationToken cancellationToken = default);
}
