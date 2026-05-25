namespace App.DAL.Contracts.Persistence;

public interface IAppUnitOfWork
{
    IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
