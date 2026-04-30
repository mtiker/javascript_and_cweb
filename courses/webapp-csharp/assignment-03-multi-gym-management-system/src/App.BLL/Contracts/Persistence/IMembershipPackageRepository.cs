using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IMembershipPackageRepository
{
    Task<IReadOnlyList<MembershipPackage>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<MembershipPackage?> FindAsync(Guid gymId, Guid packageId, CancellationToken cancellationToken = default);
    Task<bool> IsUsedAsync(Guid gymId, Guid packageId, CancellationToken cancellationToken = default);
    Task AddAsync(MembershipPackage package, CancellationToken cancellationToken = default);
    void Remove(MembershipPackage package);
}
