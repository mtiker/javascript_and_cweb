using App.Domain.Entities;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;

namespace App.BLL.Mappers;

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
