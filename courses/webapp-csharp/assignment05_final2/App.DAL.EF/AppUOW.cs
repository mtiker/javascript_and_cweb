using System.Collections.Concurrent;
using App.DAL.Contracts.Persistence;

namespace App.DAL.EF.Repositories;

public class AppUOW(AppDbContext dbContext) : IAppUnitOfWork
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class
    {
        return (IRepository<TEntity, Guid>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new EfRepository<TEntity, Guid>(_dbContext));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
