using App.BLL.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfAuthorizationQueryRepository(AppDbContext dbContext) : IAuthorizationQueryRepository
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
