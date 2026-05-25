namespace Modules.Maintenance.Application.Persistence;

public interface IMaintenancePersistenceContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
