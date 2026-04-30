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
