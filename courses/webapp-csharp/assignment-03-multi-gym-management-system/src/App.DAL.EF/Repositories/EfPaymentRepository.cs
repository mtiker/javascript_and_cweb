using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfPaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Where(payment => payment.GymId == gymId)
            .OrderByDescending(payment => payment.PaidAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> ListForMembershipOrBookingIdsAsync(
        Guid gymId,
        IReadOnlyCollection<Guid> membershipIds,
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Where(payment => payment.GymId == gymId)
            .Where(payment =>
                (payment.MembershipId.HasValue && membershipIds.Contains(payment.MembershipId.Value)) ||
                (payment.BookingId.HasValue && bookingIds.Contains(payment.BookingId.Value)))
            .OrderByDescending(payment => payment.PaidAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }
}
