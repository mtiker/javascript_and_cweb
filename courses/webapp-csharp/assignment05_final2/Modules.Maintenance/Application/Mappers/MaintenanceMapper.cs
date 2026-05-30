using System.Globalization;
using Base.Domain;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.EquipmentModels;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;

namespace Modules.Maintenance.Application.Mappers;

public sealed class MaintenanceMapper : IMaintenanceMapper
{
    public EquipmentModelResponse ToEquipmentModel(EquipmentModel entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new EquipmentModelResponse
        {
            Id = entity.Id,
            Name = Translate(entity.Name) ?? string.Empty,
            Type = entity.Type,
            Manufacturer = entity.Manufacturer,
            MaintenanceIntervalDays = entity.MaintenanceIntervalDays,
            Description = Translate(entity.Description)
        };
    }

    public IReadOnlyCollection<EquipmentModelResponse> ToEquipmentModelList(IEnumerable<EquipmentModel> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Select(ToEquipmentModel).ToArray();
    }

    public EquipmentResponse ToEquipment(Equipment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new EquipmentResponse
        {
            Id = entity.Id,
            EquipmentModelId = entity.EquipmentModelId,
            AssetTag = entity.AssetTag,
            SerialNumber = entity.SerialNumber,
            CurrentStatus = entity.CurrentStatus,
            CommissionedAt = entity.CommissionedAt,
            DecommissionedAt = entity.DecommissionedAt,
            Notes = entity.Notes
        };
    }

    public IReadOnlyCollection<EquipmentResponse> ToEquipmentList(IEnumerable<Equipment> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Select(ToEquipment).ToArray();
    }

    public MaintenanceTaskResponse ToMaintenanceTask(MaintenanceTask entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new MaintenanceTaskResponse
        {
            Id = entity.Id,
            EquipmentId = entity.EquipmentId,
            EquipmentAssetTag = entity.Equipment?.AssetTag,
            EquipmentName = Translate(entity.Equipment?.EquipmentModel?.Name) ?? entity.Equipment?.AssetTag ?? "Equipment",
            AssignedStaffId = entity.AssignedStaffId,
            AssignedStaffName = FormatStaffName(entity.AssignedStaff),
            CreatedByStaffId = entity.CreatedByStaffId,
            TaskType = entity.TaskType,
            Priority = entity.Priority,
            Status = entity.Status,
            DueAtUtc = entity.DueAtUtc,
            StartedAtUtc = entity.StartedAtUtc,
            CompletedAtUtc = entity.CompletedAtUtc,
            DowntimeStartedAtUtc = entity.DowntimeStartedAtUtc,
            DowntimeEndedAtUtc = entity.DowntimeEndedAtUtc,
            IsOverdue = entity.Status != MaintenanceTaskStatus.Done && entity.DueAtUtc.HasValue && entity.DueAtUtc.Value < DateTime.UtcNow,
            Notes = entity.Notes,
            CompletionNotes = entity.CompletionNotes
        };
    }

    public IReadOnlyCollection<MaintenanceTaskResponse> ToMaintenanceTaskList(IEnumerable<MaintenanceTask> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Select(ToMaintenanceTask).ToArray();
    }

    private static string? FormatStaffName(Staff? staff)
    {
        return staff == null
            ? null
            : $"{staff.Person?.FirstName} {staff.Person?.LastName}".Trim();
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
