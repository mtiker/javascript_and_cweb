using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;

namespace App.BLL.Services.Admin;

public sealed class AdminSessionsQueryService(IAppUnitOfWork unitOfWork) : IAdminSessionsQueryService
{
    private const int DisplayLimit = 20;

    public async Task<IReadOnlyList<AdminSessionRow>> GetSessionsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var sessions = await unitOfWork.TrainingSessions.ListWithBookingsAndTrainerByGymAsync(gymId, DisplayLimit, cancellationToken);
        return sessions
            .Select(session => new AdminSessionRow(
                session.Name,
                session.StartAtUtc,
                session.EndAtUtc,
                session.Capacity,
                session.Bookings.Count(booking => booking.Status != BookingStatus.Cancelled),
                session.Status,
                ResolveTrainerNames(session)))
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveTrainerNames(TrainingSession session) =>
        session.TrainerStaff is null
            ? []
            : [$"{session.TrainerStaff.Person?.FirstName} {session.TrainerStaff.Person?.LastName}".Trim()];
}
