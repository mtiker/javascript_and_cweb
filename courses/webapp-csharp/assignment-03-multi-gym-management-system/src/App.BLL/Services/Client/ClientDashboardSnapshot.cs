using App.Domain.Common;
using App.Domain.Enums;

namespace App.BLL.Services.Client;

public sealed record ClientDashboardSessionRow(
    Guid Id,
    Guid CategoryId,
    LangStr Name,
    LangStr? Description,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int Capacity,
    decimal BasePrice,
    string CurrencyCode,
    TrainingSessionStatus Status);

public sealed record ClientDashboardBookingRow(
    Guid Id,
    Guid TrainingSessionId,
    LangStr? TrainingSessionName,
    Guid MemberId,
    string MemberFirstName,
    string MemberLastName,
    string MemberCode,
    BookingStatus Status,
    decimal ChargedPrice,
    bool PaymentRequired);

public sealed record ClientDashboardMaintenanceTaskRow(
    Guid Id,
    Guid EquipmentId,
    string? EquipmentAssetTag,
    LangStr? EquipmentModelName,
    Guid? AssignedStaffId,
    string? AssignedStaffFirstName,
    string? AssignedStaffLastName,
    Guid? CreatedByStaffId,
    MaintenanceTaskType TaskType,
    MaintenancePriority Priority,
    MaintenanceTaskStatus Status,
    DateTime? DueAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes);

public sealed record ClientDashboardSnapshot(
    IReadOnlyList<ClientDashboardSessionRow> UpcomingSessions,
    IReadOnlyList<ClientDashboardBookingRow> MyBookings,
    IReadOnlyList<ClientDashboardMaintenanceTaskRow> AssignedTasks);
