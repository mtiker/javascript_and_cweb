using App.Domain.Entities;
using Shared.Contracts.Enums;

namespace Modules.Training.Application.Persistence;

public interface ITrainingSessionRepository
{
    Task<IReadOnlyList<TrainingSession>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSession>> ListByGymFilteredAsync(
        Guid gymId,
        TrainingSessionStatus? status,
        Guid? categoryId,
        Guid? trainerStaffId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSession>> ListWithBookingsAndTrainerByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSession>> ListUpcomingByGymAsync(Guid gymId, DateTime fromUtc, int limit, CancellationToken cancellationToken = default);

    Task<TrainingSession?> FindAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default);

    Task AddAsync(TrainingSession session, CancellationToken cancellationToken = default);

    void Remove(TrainingSession session);
}
