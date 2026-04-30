using System.Linq.Expressions;
using App.BLL.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class EfRepository<TEntity, TKey>(AppDbContext dbContext) : IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : struct
{
    private readonly AppDbContext _dbContext = dbContext;
    private DbSet<TEntity> Set => _dbContext.Set<TEntity>();

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await Set.FindAsync([id], cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = Set;
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await Set.AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }
}
