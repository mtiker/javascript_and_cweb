using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfMembershipRepository(AppDbContext dbContext) : IMembershipRepository
{
    public async Task<IReadOnlyList<Membership>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Where(membership => membership.GymId == gymId)
            .OrderByDescending(membership => membership.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Membership>> ListForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Where(membership => membership.GymId == gymId && membership.MemberId == memberId)
            .OrderByDescending(membership => membership.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Membership>> ListOverlappingAsync(Guid gymId, Guid memberId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Where(membership => membership.GymId == gymId && membership.MemberId == memberId)
            .Where(membership => membership.StartDate <= endDate && membership.EndDate >= startDate)
            .OrderByDescending(membership => membership.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Membership>> ListActiveWithDetailsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Include(membership => membership.Member)
                .ThenInclude(member => member!.Person)
            .Include(membership => membership.MembershipPackage)
            .Where(membership => membership.GymId == gymId && membership.Status == MembershipStatus.Active)
            .OrderBy(membership => membership.EndDate)
            .ToListAsync(cancellationToken);
    }

    public Task<Membership?> FindAsync(Guid gymId, Guid membershipId, CancellationToken cancellationToken = default)
    {
        return dbContext.Memberships
            .FirstOrDefaultAsync(membership => membership.GymId == gymId && membership.Id == membershipId, cancellationToken);
    }

    public Task<bool> ExistsForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return dbContext.Memberships.AnyAsync(
            membership => membership.GymId == gymId && membership.MemberId == memberId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListIdsForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Where(membership => membership.GymId == gymId && membership.MemberId == memberId)
            .Select(membership => membership.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);
        await dbContext.Memberships.AddAsync(membership, cancellationToken);
    }

    public void Remove(Membership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);
        dbContext.Memberships.Remove(membership);
    }
}
