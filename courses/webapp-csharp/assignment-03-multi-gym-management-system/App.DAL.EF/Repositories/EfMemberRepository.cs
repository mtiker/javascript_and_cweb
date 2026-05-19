using App.DAL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfMemberRepository(AppDbContext dbContext) : IMemberRepository
{
    public async Task<IReadOnlyList<Member>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Members
            .Include(member => member.Person)
            .Where(member => member.GymId == gymId)
            .OrderBy(member => member.Person!.LastName)
            .ThenBy(member => member.Person!.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<Member?> FindAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return dbContext.Members
            .FirstOrDefaultAsync(member => member.GymId == gymId && member.Id == memberId, cancellationToken);
    }

    public Task<Member?> FindWithPersonAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return dbContext.Members
            .Include(member => member.Person)
            .FirstOrDefaultAsync(member => member.GymId == gymId && member.Id == memberId, cancellationToken);
    }

    public Task<bool> MemberCodeExistsAsync(
        Guid gymId,
        string memberCode,
        Guid? excludeMemberId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberCode);
        return dbContext.Members.AnyAsync(
            member =>
                member.GymId == gymId &&
                member.MemberCode == memberCode &&
                (!excludeMemberId.HasValue || member.Id != excludeMemberId.Value),
            cancellationToken);
    }

    public Task<bool> PersonalCodeExistsAsync(
        string personalCode,
        Guid? excludePersonId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personalCode);
        return dbContext.People.AnyAsync(
            person =>
                person.PersonalCode == personalCode &&
                (!excludePersonId.HasValue || person.Id != excludePersonId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);
        await dbContext.Members.AddAsync(member, cancellationToken);
    }

    public void Remove(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);
        dbContext.Members.Remove(member);
    }
}
