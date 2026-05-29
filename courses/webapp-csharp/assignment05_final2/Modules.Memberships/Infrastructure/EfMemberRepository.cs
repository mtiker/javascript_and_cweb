using App.DAL.EF;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Modules.Memberships.Application.Persistence;

namespace Modules.Memberships.Infrastructure;

internal sealed class EfMemberRepository(AppDbContext dbContext) : IMemberRepository
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

    public async Task<IReadOnlyList<Member>> ListByGymFilteredAsync(
        Guid gymId,
        string? search,
        Shared.Contracts.Enums.MemberStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Members
            .Include(member => member.Person)
            .Where(member => member.GymId == gymId);

        if (status.HasValue)
        {
            query = query.Where(member => member.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(member =>
                Microsoft.EntityFrameworkCore.EF.Functions.Like(member.MemberCode.ToLower(), $"%{term}%") ||
                Microsoft.EntityFrameworkCore.EF.Functions.Like(member.Person!.FirstName.ToLower(), $"%{term}%") ||
                Microsoft.EntityFrameworkCore.EF.Functions.Like(member.Person!.LastName.ToLower(), $"%{term}%"));
        }

        return await query
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

    public async Task<int> GetMaxMemberCodeSequenceAsync(
        Guid gymId,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var suffixes = await dbContext.Members
            .Where(member => member.GymId == gymId && member.MemberCode.StartsWith(prefix))
            .Select(member => member.MemberCode.Substring(prefix.Length))
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var suffix in suffixes)
        {
            if (int.TryParse(suffix, out var value) && value > max)
            {
                max = value;
            }
        }

        return max;
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
