using App.DAL.EF;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Enums;

namespace Modules.Memberships.Infrastructure;

public sealed class EfMembershipRepository(AppDbContext dbContext) : IMembershipRepository
{
    public async Task<IReadOnlyList<Membership>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Memberships
            .Where(membership => membership.GymId == gymId)
            .OrderByDescending(membership => membership.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Membership>> ListByGymFilteredAsync(
        Guid gymId,
        MembershipStatus? status,
        Guid? memberId,
        Guid? membershipPackageId,
        DateOnly? startFrom,
        DateOnly? startTo,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Memberships.Where(membership => membership.GymId == gymId);

        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (memberId.HasValue) query = query.Where(m => m.MemberId == memberId.Value);
        if (membershipPackageId.HasValue) query = query.Where(m => m.MembershipPackageId == membershipPackageId.Value);
        if (startFrom.HasValue) query = query.Where(m => m.StartDate >= startFrom.Value);
        if (startTo.HasValue) query = query.Where(m => m.StartDate <= startTo.Value);

        return await query
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
