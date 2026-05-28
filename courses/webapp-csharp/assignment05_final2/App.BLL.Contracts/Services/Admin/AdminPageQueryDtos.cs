using Base.Domain;
using Shared.Contracts.Enums;

namespace App.BLL.Contracts.Services.Admin;

public sealed record AdminEquipmentRow(string AssetTag, LangStr? ModelName, EquipmentStatus Status);

public sealed record AdminMaintenanceTaskRow(
    string AssetTag,
    MaintenanceTaskType TaskType,
    MaintenanceTaskStatus Status,
    string? AssignedTo,
    DateTime? DueAtUtc);

public sealed record AdminOperationsSnapshot(
    IReadOnlyList<AdminEquipmentRow> Equipment,
    IReadOnlyList<AdminMaintenanceTaskRow> MaintenanceTasks);

public sealed record AdminSessionRow(
    Guid Id,
    LangStr Name,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int Capacity,
    int BookingCount,
    TrainingSessionStatus Status,
    IReadOnlyList<string> TrainerNames);
