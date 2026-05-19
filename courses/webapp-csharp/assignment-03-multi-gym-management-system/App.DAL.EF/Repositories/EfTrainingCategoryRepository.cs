using App.DAL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfTrainingCategoryRepository(AppDbContext dbContext) : ITrainingCategoryRepository
{
    public async Task<IReadOnlyList<TrainingCategory>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingCategories
            .Where(category => category.GymId == gymId)
            .OrderBy(category => category.ValidFrom)
            .ToListAsync(cancellationToken);
    }

    public Task<TrainingCategory?> FindAsync(Guid gymId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        return dbContext.TrainingCategories
            .FirstOrDefaultAsync(category => category.GymId == gymId && category.Id == categoryId, cancellationToken);
    }

    public async Task AddAsync(TrainingCategory category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        await dbContext.TrainingCategories.AddAsync(category, cancellationToken);
    }

    public void Remove(TrainingCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        dbContext.TrainingCategories.Remove(category);
    }
}
