using App.Domain.Entities;
using Shared.Contracts.Enums;

namespace Modules.Memberships.Application.Persistence;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> ListByGymFilteredAsync(
        Guid gymId,
        PaymentStatus? status,
        Guid? membershipId,
        Guid? bookingId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> ListForMembershipOrBookingIdsAsync(
        Guid gymId,
        IReadOnlyCollection<Guid> membershipIds,
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default);
    Task<Payment?> FindAsync(Guid gymId, Guid paymentId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
