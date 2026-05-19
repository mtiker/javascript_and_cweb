using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;

namespace App.BLL.Contracts.Services;

public interface IMaintenanceWorkflowService
{
    Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default);
    Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default);
    Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default);
    Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default);
}
