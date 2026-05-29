using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Modules.Gyms.Application.Persistence;

namespace Modules.Gyms.Infrastructure;

internal sealed class EfAuthorizationQueryRepository(AppDbContext dbContext) : IAuthorizationQueryRepository
{
    public Task<AuthorizationGymLookup?> FindGymByCodeAsync(
        string gymCode,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Gyms
            .AsNoTracking()
            .Where(gym => gym.Code == gymCode)
            .Select(gym => new AuthorizationGymLookup(gym.Id, gym.Code))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
