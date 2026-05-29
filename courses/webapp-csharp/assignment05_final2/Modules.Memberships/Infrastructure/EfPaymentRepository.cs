using App.DAL.EF;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Enums;

namespace Modules.Memberships.Infrastructure;

internal sealed class EfPaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Where(payment => payment.GymId == gymId)
            .OrderByDescending(payment => payment.PaidAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> ListByGymFilteredAsync(
        Guid gymId,
        PaymentStatus? status,
        Guid? membershipId,
        Guid? bookingId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Payments.Where(payment => payment.GymId == gymId);

        if (status.HasValue) query = query.Where(payment => payment.Status == status.Value);
        if (membershipId.HasValue) query = query.Where(payment => payment.MembershipId == membershipId.Value);
        if (bookingId.HasValue) query = query.Where(payment => payment.BookingId == bookingId.Value);
        if (fromUtc.HasValue) query = query.Where(payment => payment.PaidAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(payment => payment.PaidAtUtc <= toUtc.Value);

        return await query
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

    public Task<Payment?> FindAsync(Guid gymId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        return dbContext.Payments.FirstOrDefaultAsync(p => p.GymId == gymId && p.Id == paymentId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }
}
