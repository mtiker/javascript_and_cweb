using App.DAL.Contracts.Persistence;
using App.BLL.Contracts.Services.Client;
using SharedKernel.Exceptions;
using App.Domain.Entities;
using Modules.Training.Application.Persistence;

namespace WebApp.Areas.Client.Queries;

public sealed class ClientSessionsQueryService(
    IAppUnitOfWork unitOfWork,
    ITrainingSessionRepository trainingSessionRepository,
    IBookingRepository bookingRepository) : IClientSessionsQueryService
{
    public async Task<ClientSessionDetailSnapshot> GetDetailSnapshotAsync(
        Guid gymId,
        Guid sessionId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default)
    {
        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");

        var category = (await unitOfWork.Repository<TrainingCategory>().ListAsync(
                entity => entity.GymId == gymId && entity.Id == session.CategoryId,
                cancellationToken))
            .FirstOrDefault();

        var trainerNames = session.TrainerStaff is null
            ? Array.Empty<string>()
            : [$"{session.TrainerStaff.Person?.FirstName} {session.TrainerStaff.Person?.LastName}".Trim()];

        ClientSessionBookingState? currentBooking = null;
        if (currentMemberId.HasValue)
        {
            var booking = await bookingRepository.FindActiveForMemberSessionAsync(
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
        _ = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
            ?? throw new NotFoundException("Training session was not found.");

        var bookings = await bookingRepository.ListForSessionAsync(gymId, sessionId, cancellationToken);

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

    public async Task<bool> HasTrainerAssignmentAsync(
        Guid gymId,
        Guid sessionId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken);
        return session?.TrainerStaffId == staffId;
    }
}
