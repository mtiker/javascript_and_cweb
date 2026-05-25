using App.Domain.Entities;

namespace Modules.Training.Application.Persistence;

public interface ITrainingCategoryRepository
{
    Task<IReadOnlyList<TrainingCategory>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<TrainingCategory?> FindAsync(Guid gymId, Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(TrainingCategory category, CancellationToken cancellationToken = default);

    void Remove(TrainingCategory category);
}
