namespace Modules.Gyms.Application.Persistence;

public interface IGymsTenantPersistenceContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
