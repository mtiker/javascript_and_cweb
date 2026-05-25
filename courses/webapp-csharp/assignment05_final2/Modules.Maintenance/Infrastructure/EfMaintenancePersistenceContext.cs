using App.DAL.EF;
using Modules.Maintenance.Application.Persistence;

namespace Modules.Maintenance.Infrastructure;

public sealed class EfMaintenancePersistenceContext(AppDbContext dbContext) : IMaintenancePersistenceContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
