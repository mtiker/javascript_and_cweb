using App.DAL.EF;
using Modules.Gyms.Application.Persistence;

namespace Modules.Gyms.Infrastructure;

internal sealed class EfGymsTenantPersistenceContext(AppDbContext dbContext) : IGymsTenantPersistenceContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
