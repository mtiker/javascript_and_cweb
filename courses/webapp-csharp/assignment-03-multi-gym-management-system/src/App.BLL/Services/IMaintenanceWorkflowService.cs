using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;

namespace App.BLL.Services;

public interface IMaintenanceWorkflowService
{
    Task<IReadOnlyCollection<OpeningHoursResponse>> GetOpeningHoursAsync(string gymCode);
    Task<OpeningHoursResponse> CreateOpeningHoursAsync(string gymCode, OpeningHoursUpsertRequest request);
    Task<OpeningHoursResponse> UpdateOpeningHoursAsync(string gymCode, Guid id, OpeningHoursUpsertRequest request);
    Task DeleteOpeningHoursAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> GetOpeningHourExceptionsAsync(string gymCode);
    Task<OpeningHoursExceptionResponse> CreateOpeningHourExceptionAsync(string gymCode, OpeningHoursExceptionUpsertRequest request);
    Task<OpeningHoursExceptionResponse> UpdateOpeningHourExceptionAsync(string gymCode, Guid id, OpeningHoursExceptionUpsertRequest request);
    Task DeleteOpeningHourExceptionAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode);
    Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request);
    Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request);
    Task DeleteEquipmentModelAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode);
    Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request);
    Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request);
    Task DeleteEquipmentAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode);
    Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request);
    Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request);
    Task<int> GenerateDueScheduledTasksAsync(string gymCode);
    Task DeleteMaintenanceTaskAsync(string gymCode, Guid id);
    Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode);
    Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request);
    Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode);
    Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request);
    Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName);
}
