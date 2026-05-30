using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.EquipmentModels;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;

namespace Modules.Maintenance.Application.Mappers;

public interface IMaintenanceMapper
{
    EquipmentModelResponse ToEquipmentModel(EquipmentModel entity);
    IReadOnlyCollection<EquipmentModelResponse> ToEquipmentModelList(IEnumerable<EquipmentModel> entities);
    EquipmentResponse ToEquipment(Equipment entity);
    IReadOnlyCollection<EquipmentResponse> ToEquipmentList(IEnumerable<Equipment> entities);
    MaintenanceTaskResponse ToMaintenanceTask(MaintenanceTask entity, string? assignedStaffName);
    IReadOnlyCollection<MaintenanceTaskResponse> ToMaintenanceTaskList(
        IEnumerable<MaintenanceTask> entities,
        IReadOnlyDictionary<Guid, string> staffNameByStaffId);
}
