using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Modules.Training.Application.Persistence;

namespace Modules.Training.Infrastructure;

internal sealed class EfTrainingSessionRepository(AppDbContext dbContext) : ITrainingSessionRepository
{
    public async Task<IReadOnlyList<TrainingSession>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .Include(session => session.TrainerStaff)
                .ThenInclude(staff => staff!.Person)
            .Where(session => session.GymId == gymId)
            .OrderBy(session => session.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSession>> ListByGymFilteredAsync(
        Guid gymId,
        TrainingSessionStatus? status,
        Guid? categoryId,
        Guid? trainerStaffId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TrainingSessions
            .Include(session => session.TrainerStaff)
                .ThenInclude(staff => staff!.Person)
            .Where(session => session.GymId == gymId);

        if (status.HasValue)
        {
            query = query.Where(session => session.Status == status.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(session => session.CategoryId == categoryId.Value);
        }

        if (trainerStaffId.HasValue)
        {
            query = query.Where(session => session.TrainerStaffId == trainerStaffId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(session => session.StartAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(session => session.StartAtUtc <= toUtc.Value);
        }

        return await query
            .OrderBy(session => session.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSession>> ListWithBookingsAndTrainerByGymAsync(Guid gymId, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .AsNoTracking()
            .Include(session => session.Bookings)
            .Include(session => session.TrainerStaff)
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
            .Include(session => session.TrainerStaff)
                .ThenInclude(staff => staff!.Person)
            .Where(session => session.GymId == gymId && session.StartAtUtc >= fromUtc)
            .OrderBy(session => session.StartAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<TrainingSession?> FindAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.TrainingSessions
            .Include(session => session.TrainerStaff)
                .ThenInclude(staff => staff!.Person)
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
