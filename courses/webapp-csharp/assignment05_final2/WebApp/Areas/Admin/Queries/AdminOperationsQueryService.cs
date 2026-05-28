using App.BLL.Contracts.Services.Admin;
using App.Domain.Entities;
using Modules.Maintenance.Application.Persistence;

namespace WebApp.Areas.Admin.Queries;

public sealed class AdminOperationsQueryService(IMaintenanceRepository maintenanceRepository) : IAdminOperationsQueryService
{
    private const int DisplayLimit = 20;

    public async Task<AdminOperationsSnapshot> GetSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var equipment = (await maintenanceRepository.ListEquipmentWithModelByGymAsync(gymId, DisplayLimit, cancellationToken))
            .Select(entity => new AdminEquipmentRow(
                entity.Id,
                ResolveAssetTag(entity),
                entity.EquipmentModel?.Name,
                entity.CurrentStatus))
            .ToArray();

        var maintenanceTasks = (await maintenanceRepository.ListIncompleteMaintenanceTasksWithStaffByGymAsync(gymId, DisplayLimit, cancellationToken))
            .Select(entity => new AdminMaintenanceTaskRow(
                entity.Id,
                ResolveTaskAssetTag(entity),
                entity.TaskType,
                entity.Status,
                ResolveAssignedTo(entity.AssignedStaff),
                entity.DueAtUtc))
            .ToArray();

        return new AdminOperationsSnapshot(equipment, maintenanceTasks);
    }

    private static string ResolveAssetTag(Equipment equipment) =>
        equipment.AssetTag ?? equipment.SerialNumber ?? equipment.Id.ToString();

    private static string ResolveTaskAssetTag(MaintenanceTask task) =>
        task.Equipment?.AssetTag ?? task.Equipment?.SerialNumber ?? task.EquipmentId.ToString();

    private static string? ResolveAssignedTo(Staff? staff)
    {
        if (staff is null)
        {
            return null;
        }

        return $"{staff.Person?.FirstName} {staff.Person?.LastName}".Trim();
    }
}
