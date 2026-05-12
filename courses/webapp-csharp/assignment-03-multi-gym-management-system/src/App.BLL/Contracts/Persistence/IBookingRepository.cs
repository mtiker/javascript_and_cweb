using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListRecentForMemberAsync(Guid gymId, Guid memberId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListForTrainerAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken = default);

    Task<Booking?> FindAsync(Guid gymId, Guid bookingId, CancellationToken cancellationToken = default);

    Task<Booking?> FindWithTrainingSessionAndMemberAsync(Guid gymId, Guid bookingId, CancellationToken cancellationToken = default);

    Task<Booking?> FindActiveForMemberSessionAsync(Guid gymId, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForMemberSessionAsync(Guid gymId, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<int> CountActiveForSessionAsync(Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
}
