using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfTrainingSessionRepository(AppDbContext dbContext) : ITrainingSessionRepository
{
    public async Task<IReadOnlyList<TrainingSession>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .Where(session => session.GymId == gymId)
            .OrderBy(session => session.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSession>> ListWithBookingsAndShiftsByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .AsNoTracking()
            .Include(session => session.Bookings)
            .Include(session => session.WorkShifts)
                .ThenInclude(shift => shift.Contract)
                    .ThenInclude(contract => contract!.Staff)
                        .ThenInclude(staff => staff!.Person)
            .Where(session => session.GymId == gymId)
            .OrderBy(session => session.StartAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSession>> ListUpcomingByGymAsync(Guid gymId, DateTime fromUtc, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .AsNoTracking()
            .Where(session => session.GymId == gymId && session.StartAtUtc >= fromUtc)
            .OrderBy(session => session.StartAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<TrainingSession?> FindAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.TrainingSessions
            .FirstOrDefaultAsync(session => session.GymId == gymId && session.Id == sessionId, cancellationToken);
    }

    public async Task AddAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await dbContext.TrainingSessions.AddAsync(session, cancellationToken);
    }

    public void Remove(TrainingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        dbContext.TrainingSessions.Remove(session);
    }
}
