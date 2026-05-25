using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.EquipmentModels;
using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;

namespace Modules.Maintenance.Application.Mappers;

public interface IMaintenanceMapper
{
    EquipmentModelResponse ToEquipmentModel(EquipmentModel entity);
    IReadOnlyCollection<EquipmentModelResponse> ToEquipmentModelList(IEnumerable<EquipmentModel> entities);
    EquipmentResponse ToEquipment(Equipment entity);
    IReadOnlyCollection<EquipmentResponse> ToEquipmentList(IEnumerable<Equipment> entities);
    GymSettingsResponse ToGymSettings(GymSettings entity);
    GymUserResponse ToGymUser(AppUserGymRole entity);
    IReadOnlyCollection<GymUserResponse> ToGymUserList(IEnumerable<AppUserGymRole> entities);
    MaintenanceTaskResponse ToMaintenanceTask(MaintenanceTask entity);
    IReadOnlyCollection<MaintenanceTaskResponse> ToMaintenanceTaskList(IEnumerable<MaintenanceTask> entities);
}
