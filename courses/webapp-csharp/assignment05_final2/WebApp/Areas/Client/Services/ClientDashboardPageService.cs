using System.Globalization;
using App.BLL.Contracts.Services;
using App.BLL.Contracts.Services.Client;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Shared.Contracts.Dtos.v1.TrainingSessions;
using WebApp.Models;

namespace WebApp.Areas.Client.Services;

public interface IClientDashboardPageService
{
    Task<ClientDashboardViewModel?> BuildAsync(CancellationToken cancellationToken = default);
}

public sealed class ClientDashboardPageService(
    IUserContextService userContextService,
    IAuthorizationService authorizationService,
    IClientDashboardQueryService dashboardQueryService) : IClientDashboardPageService
{
    public async Task<ClientDashboardViewModel?> BuildAsync(CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return null;
        }

        var gymId = context.ActiveGymId.Value;
        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        var currentStaff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken);

        var snapshot = await dashboardQueryService.GetSnapshotAsync(
            gymId,
            currentMember?.Id,
            currentStaff?.Id,
            cancellationToken);

        var culture = CultureInfo.CurrentUICulture.Name;

        return new ClientDashboardViewModel
        {
            ActiveGymCode = context.ActiveGymCode,
            ActiveRole = context.ActiveRole,
            UpcomingSessions = snapshot.UpcomingSessions
                .Select(row => new TrainingSessionResponse
                {
                    Id = row.Id,
                    CategoryId = row.CategoryId,
                    Name = row.Name.Translate(culture) ?? string.Empty,
                    Description = row.Description?.Translate(culture),
                    StartAtUtc = row.StartAtUtc,
                    EndAtUtc = row.EndAtUtc,
                    Capacity = row.Capacity,
                    BasePrice = row.BasePrice,
                    CurrencyCode = row.CurrencyCode,
                    Status = row.Status
                })
                .ToArray(),
            MyBookings = snapshot.MyBookings
                .Select(row => new BookingResponse
                {
                    Id = row.Id,
                    TrainingSessionId = row.TrainingSessionId,
                    TrainingSessionName = row.TrainingSessionName?.Translate(culture) ?? string.Empty,
                    MemberId = row.MemberId,
                    MemberName = $"{row.MemberFirstName} {row.MemberLastName}".Trim(),
                    MemberCode = row.MemberCode,
                    Status = row.Status,
                    ChargedPrice = row.ChargedPrice,
                    PaymentRequired = row.PaymentRequired
                })
                .ToArray(),
            AssignedTasks = snapshot.AssignedTasks
                .Select(row => new MaintenanceTaskResponse
                {
                    Id = row.Id,
                    EquipmentId = row.EquipmentId,
                    EquipmentAssetTag = row.EquipmentAssetTag,
                    EquipmentName = row.EquipmentModelName?.Translate(culture) ?? row.EquipmentAssetTag ?? "Equipment",
                    AssignedStaffId = row.AssignedStaffId,
                    AssignedStaffName = row.AssignedStaffFirstName is null && row.AssignedStaffLastName is null
                        ? null
                        : $"{row.AssignedStaffFirstName} {row.AssignedStaffLastName}".Trim(),
                    CreatedByStaffId = row.CreatedByStaffId,
                    TaskType = row.TaskType,
                    Priority = row.Priority,
                    Status = row.Status,
                    DueAtUtc = row.DueAtUtc,
                    StartedAtUtc = row.StartedAtUtc,
                    CompletedAtUtc = row.CompletedAtUtc,
                    Notes = row.Notes
                })
                .ToArray()
        };
    }
}
