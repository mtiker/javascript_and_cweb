using App.BLL.Contracts.Services.Admin;
using App.Domain.Entities;
using Modules.Training.Application.Persistence;
using Shared.Contracts.Enums;

namespace WebApp.Areas.Admin.Queries;

public sealed class AdminSessionsQueryService(ITrainingSessionRepository trainingSessionRepository) : IAdminSessionsQueryService
{
    private const int DisplayLimit = 20;

    public async Task<IReadOnlyList<AdminSessionRow>> GetSessionsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var sessions = await trainingSessionRepository.ListWithBookingsAndTrainerByGymAsync(gymId, DisplayLimit, cancellationToken);
        return sessions
            .Select(session => new AdminSessionRow(
                session.Id,
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
