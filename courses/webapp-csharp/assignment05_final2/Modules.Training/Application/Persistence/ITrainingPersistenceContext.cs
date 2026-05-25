namespace Modules.Training.Application.Persistence;

public interface ITrainingPersistenceContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
