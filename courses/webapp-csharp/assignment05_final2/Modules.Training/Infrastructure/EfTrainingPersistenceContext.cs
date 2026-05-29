using App.DAL.EF;
using Modules.Training.Application.Persistence;

namespace Modules.Training.Infrastructure;

internal sealed class EfTrainingPersistenceContext(AppDbContext dbContext) : ITrainingPersistenceContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
