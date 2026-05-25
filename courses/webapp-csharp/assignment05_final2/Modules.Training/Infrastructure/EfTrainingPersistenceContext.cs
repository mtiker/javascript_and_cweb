using App.DAL.EF;
using Modules.Training.Application.Persistence;

namespace Modules.Training.Infrastructure;

public sealed class EfTrainingPersistenceContext(AppDbContext dbContext) : ITrainingPersistenceContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
