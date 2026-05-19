using App.Domain.Entities;
using App.Domain.Enums;

namespace App.DAL.Contracts.Persistence;

public interface IMemberRepository
{
    Task<IReadOnlyList<Member>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Member>> ListByGymFilteredAsync(
        Guid gymId,
        string? search,
        MemberStatus? status,
        CancellationToken cancellationToken = default);

    Task<Member?> FindAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);

    Task<Member?> FindWithPersonAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);

    Task<bool> MemberCodeExistsAsync(
        Guid gymId,
        string memberCode,
        Guid? excludeMemberId,
        CancellationToken cancellationToken = default);

    Task<bool> PersonalCodeExistsAsync(
        string personalCode,
        Guid? excludePersonId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Member member, CancellationToken cancellationToken = default);

    void Remove(Member member);
}
