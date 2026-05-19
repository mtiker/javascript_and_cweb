using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface ITrainingSessionRepository
{
    Task<IReadOnlyList<TrainingSession>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSession>> ListWithBookingsAndTrainerByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSession>> ListUpcomingByGymAsync(Guid gymId, DateTime fromUtc, int limit, CancellationToken cancellationToken = default);

    Task<TrainingSession?> FindAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default);

    Task AddAsync(TrainingSession session, CancellationToken cancellationToken = default);

    void Remove(TrainingSession session);
}
