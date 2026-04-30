using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> ListForMembershipOrBookingIdsAsync(
        Guid gymId,
        IReadOnlyCollection<Guid> membershipIds,
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
