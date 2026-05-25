using App.BLL.Contracts.Services.Client;
using Modules.Maintenance.Application.Persistence;
using Modules.Training.Application.Persistence;

namespace WebApp.Areas.Client.Queries;

public sealed class ClientDashboardQueryService(
    IMaintenanceRepository maintenanceRepository,
    ITrainingSessionRepository trainingSessionRepository,
    IBookingRepository bookingRepository) : IClientDashboardQueryService
{
    private const int DisplayLimit = 5;

    public async Task<ClientDashboardSnapshot> GetSnapshotAsync(
        Guid gymId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default)
    {
        var sessionEntities = await trainingSessionRepository.ListUpcomingByGymAsync(gymId, DateTime.UtcNow, DisplayLimit, cancellationToken);
        var upcomingSessions = sessionEntities
            .Select(session => new ClientDashboardSessionRow(
                session.Id,
                session.CategoryId,
                session.Name,
                session.Description,
                session.StartAtUtc,
                session.EndAtUtc,
                session.Capacity,
                session.BasePrice,
                session.CurrencyCode,
                session.Status))
            .ToArray();

        IReadOnlyList<ClientDashboardBookingRow> myBookings = Array.Empty<ClientDashboardBookingRow>();
        if (currentMemberId.HasValue)
        {
            var bookingEntities = await bookingRepository.ListRecentForMemberAsync(gymId, currentMemberId.Value, DisplayLimit, cancellationToken);
            myBookings = bookingEntities
                .Select(booking => new ClientDashboardBookingRow(
                    booking.Id,
                    booking.TrainingSessionId,
                    booking.TrainingSession?.Name,
                    booking.MemberId,
                    booking.Member?.Person?.FirstName ?? string.Empty,
                    booking.Member?.Person?.LastName ?? string.Empty,
                    booking.Member?.MemberCode ?? string.Empty,
                    booking.Status,
                    booking.ChargedPrice,
                    booking.PaymentRequired))
                .ToArray();
        }

        IReadOnlyList<ClientDashboardMaintenanceTaskRow> assignedTasks = Array.Empty<ClientDashboardMaintenanceTaskRow>();
        if (currentStaffId.HasValue)
        {
            var taskEntities = await maintenanceRepository.ListAssignedTasksWithEquipmentByStaffAsync(gymId, currentStaffId.Value, DisplayLimit, cancellationToken);
            assignedTasks = taskEntities
                .Select(task => new ClientDashboardMaintenanceTaskRow(
                    task.Id,
                    task.EquipmentId,
                    task.Equipment?.AssetTag,
                    task.Equipment?.EquipmentModel?.Name,
                    task.AssignedStaffId,
                    task.AssignedStaff?.Person?.FirstName,
                    task.AssignedStaff?.Person?.LastName,
                    task.CreatedByStaffId,
                    task.TaskType,
                    task.Priority,
                    task.Status,
                    task.DueAtUtc,
                    task.StartedAtUtc,
                    task.CompletedAtUtc,
                    task.Notes))
                .ToArray();
        }

        return new ClientDashboardSnapshot(upcomingSessions, myBookings, assignedTasks);
    }
}
