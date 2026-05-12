using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.Domain.Entities;

namespace App.BLL.Services.Client;

public sealed class ClientSessionsQueryService(IAppUnitOfWork unitOfWork) : IClientSessionsQueryService
{
    public async Task<ClientSessionDetailSnapshot> GetDetailSnapshotAsync(
        Guid gymId,
        Guid sessionId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default)
    {
        var session = await unitOfWork.TrainingSessions.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");

        var category = (await unitOfWork.Repository<TrainingCategory>().ListAsync(
                entity => entity.GymId == gymId && entity.Id == session.CategoryId,
                cancellationToken))
            .FirstOrDefault();

        var trainerShifts = await unitOfWork.WorkShifts.ListTrainingShiftsWithStaffForSessionAsync(
            gymId,
            sessionId,
            cancellationToken);

        var trainerNames = trainerShifts
            .Select(ResolveStaffName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        ClientSessionBookingState? currentBooking = null;
        if (currentMemberId.HasValue)
        {
            var booking = await unitOfWork.Bookings.FindActiveForMemberSessionAsync(
                gymId,
                currentMemberId.Value,
                sessionId,
                cancellationToken);

            if (booking is not null)
            {
                currentBooking = new ClientSessionBookingState(booking.Id, booking.Status);
            }
        }

        var currentStaffCanManageRoster = currentStaffId.HasValue &&
                                          await HasTrainerAssignmentAsync(gymId, sessionId, currentStaffId.Value, cancellationToken);

        return new ClientSessionDetailSnapshot(
            category?.Name,
            trainerNames,
            currentBooking,
            currentStaffCanManageRoster);
    }

    public async Task<IReadOnlyList<ClientSessionRosterRow>> GetRosterBookingsAsync(
        Guid gymId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _ = await unitOfWork.TrainingSessions.FindAsync(gymId, sessionId, cancellationToken)
            ?? throw new NotFoundException("Training session was not found.");

        var bookings = await unitOfWork.Bookings.ListForSessionAsync(gymId, sessionId, cancellationToken);

        return bookings
            .Select(booking => new ClientSessionRosterRow(
                booking.Id,
                booking.Member?.Person?.FirstName ?? string.Empty,
                booking.Member?.Person?.LastName ?? string.Empty,
                booking.Status,
                booking.ChargedPrice,
                booking.PaymentRequired))
            .ToArray();
    }

    public Task<bool> HasTrainerAssignmentAsync(
        Guid gymId,
        Guid sessionId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        return unitOfWork.WorkShifts.ExistsTrainingShiftForStaffAsync(gymId, sessionId, staffId, cancellationToken);
    }

    private static string ResolveStaffName(WorkShift shift)
    {
        var person = shift.Contract?.Staff?.Person;
        return person is null
            ? string.Empty
            : $"{person.FirstName} {person.LastName}".Trim();
    }
}
