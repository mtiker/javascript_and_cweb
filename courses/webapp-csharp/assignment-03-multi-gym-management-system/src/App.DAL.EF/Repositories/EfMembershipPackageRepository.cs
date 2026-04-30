using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfMembershipPackageRepository(AppDbContext dbContext) : IMembershipPackageRepository
{
    public async Task<IReadOnlyList<MembershipPackage>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MembershipPackages
            .Where(package => package.GymId == gymId)
            .OrderBy(package => package.ValidFrom)
            .ThenBy(package => package.BasePrice)
            .ToListAsync(cancellationToken);
    }

    public Task<MembershipPackage?> FindAsync(Guid gymId, Guid packageId, CancellationToken cancellationToken = default)
    {
        return dbContext.MembershipPackages
            .FirstOrDefaultAsync(package => package.GymId == gymId && package.Id == packageId, cancellationToken);
    }

    public Task<bool> IsUsedAsync(Guid gymId, Guid packageId, CancellationToken cancellationToken = default)
    {
        return dbContext.Memberships.AnyAsync(
            membership => membership.GymId == gymId && membership.MembershipPackageId == packageId,
            cancellationToken);
    }

    public async Task AddAsync(MembershipPackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        await dbContext.MembershipPackages.AddAsync(package, cancellationToken);
    }

    public void Remove(MembershipPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        dbContext.MembershipPackages.Remove(package);
    }
}
