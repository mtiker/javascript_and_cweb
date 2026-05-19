using App.Domain.Entities;
using App.Domain.Enums;

namespace App.DAL.Contracts.Persistence;

public interface IMembershipRepository
{
    Task<IReadOnlyList<Membership>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Membership>> ListByGymFilteredAsync(
        Guid gymId,
        MembershipStatus? status,
        Guid? memberId,
        Guid? membershipPackageId,
        DateOnly? startFrom,
        DateOnly? startTo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> ListForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> ListOverlappingAsync(Guid gymId, Guid memberId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> ListActiveWithDetailsAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<Membership?> FindAsync(Guid gymId, Guid membershipId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListIdsForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task AddAsync(Membership membership, CancellationToken cancellationToken = default);
    void Remove(Membership membership);
}
