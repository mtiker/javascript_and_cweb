using App.DAL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfBookingRepository(AppDbContext dbContext) : IBookingRepository
{
    public async Task<IReadOnlyList<Booking>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await BaseListQuery(gymId)
            .OrderByDescending(booking => booking.BookedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return await BaseListQuery(gymId)
            .AsNoTracking()
            .Where(booking => booking.TrainingSessionId == trainingSessionId)
            .OrderBy(booking => booking.Member!.Person!.LastName)
            .ThenBy(booking => booking.Member!.Person!.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await BaseListQuery(gymId)
            .Where(booking => booking.MemberId == memberId)
            .OrderByDescending(booking => booking.BookedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListRecentForMemberAsync(Guid gymId, Guid memberId, int limit, CancellationToken cancellationToken = default)
    {
        return await BaseListQuery(gymId)
            .AsNoTracking()
            .Where(booking => booking.MemberId == memberId)
            .OrderByDescending(booking => booking.BookedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListForTrainerAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken = default)
    {
        return await BaseListQuery(gymId)
            .Where(booking => booking.TrainingSession != null &&
                              booking.TrainingSession.TrainerStaffId == staffId)
            .OrderByDescending(booking => booking.BookedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Booking?> FindAsync(Guid gymId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings
            .FirstOrDefaultAsync(booking => booking.GymId == gymId && booking.Id == bookingId, cancellationToken);
    }

    public Task<Booking?> FindWithTrainingSessionAndMemberAsync(Guid gymId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        return BaseListQuery(gymId)
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);
    }

    public Task<Booking?> FindActiveForMemberSessionAsync(Guid gymId, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                booking =>
                    booking.GymId == gymId &&
                    booking.MemberId == memberId &&
                    booking.TrainingSessionId == trainingSessionId &&
                    booking.Status != BookingStatus.Cancelled,
                cancellationToken);
    }

    public Task<bool> ExistsForMemberSessionAsync(Guid gymId, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.AnyAsync(
            booking =>
                booking.GymId == gymId &&
                booking.MemberId == memberId &&
                booking.TrainingSessionId == trainingSessionId &&
                booking.Status != BookingStatus.Cancelled,
            cancellationToken);
    }

    public Task<int> CountActiveForSessionAsync(Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.CountAsync(
            booking =>
                booking.TrainingSessionId == trainingSessionId &&
                booking.Status == BookingStatus.Booked,
            cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    private IQueryable<Booking> BaseListQuery(Guid gymId)
    {
        return dbContext.Bookings
            .Include(booking => booking.TrainingSession)
            .Include(booking => booking.Member)
                .ThenInclude(member => member!.Person)
            .Where(booking => booking.GymId == gymId);
    }
}
