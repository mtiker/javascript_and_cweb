using App.Domain.Entities;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;

namespace App.BLL.Mapping;

public interface IMaintenanceMapper
{
    OpeningHoursResponse ToOpeningHours(OpeningHours entity);
    IReadOnlyCollection<OpeningHoursResponse> ToOpeningHoursList(IEnumerable<OpeningHours> entities);
    OpeningHoursExceptionResponse ToOpeningHoursException(OpeningHoursException entity);
    IReadOnlyCollection<OpeningHoursExceptionResponse> ToOpeningHoursExceptionList(IEnumerable<OpeningHoursException> entities);
    EquipmentModelResponse ToEquipmentModel(EquipmentModel entity);
    IReadOnlyCollection<EquipmentModelResponse> ToEquipmentModelList(IEnumerable<EquipmentModel> entities);
    EquipmentResponse ToEquipment(Equipment entity);
    IReadOnlyCollection<EquipmentResponse> ToEquipmentList(IEnumerable<Equipment> entities);
    GymSettingsResponse ToGymSettings(GymSettings entity);
    GymUserResponse ToGymUser(AppUserGymRole entity);
    IReadOnlyCollection<GymUserResponse> ToGymUserList(IEnumerable<AppUserGymRole> entities);
    MaintenanceTaskResponse ToMaintenanceTask(MaintenanceTask entity);
    IReadOnlyCollection<MaintenanceTaskResponse> ToMaintenanceTaskList(IEnumerable<MaintenanceTask> entities);
    MaintenanceTaskAssignmentHistoryResponse ToAssignmentHistory(MaintenanceTaskAssignmentHistory entity);
    IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse> ToAssignmentHistoryList(IEnumerable<MaintenanceTaskAssignmentHistory> entities);
}
