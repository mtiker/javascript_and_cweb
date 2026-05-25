using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.ModuleApis;

namespace Modules.Memberships.Application;

/// <summary>
/// Phase 6 implementation of <see cref="IMembershipsModuleApi"/>. Resolves
/// member identity from the shared <see cref="AppDbContext"/>
/// (Members + Person + AppUser). Returns <see cref="MemberSummary"/>
/// projections only — never the underlying <c>Member</c>, <c>Person</c>, or
/// <c>AppUser</c> entities. The shared context dependency is transitional
/// (Phase 6-9); Phase 9 splits per-module persistence and Phase 10 drops the
/// legacy <c>App.*</c> references.
/// </summary>
internal sealed class MembershipsModuleApiService(AppDbContext dbContext) : IMembershipsModuleApi
{
    public async Task<MemberSummary?> GetMemberSummaryAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Members
            .AsNoTracking()
            .Where(member => member.GymId == gymId && member.Id == memberId)
            .Select(member => new
            {
                member.Id,
                member.GymId,
                member.MemberCode,
                FirstName = member.Person!.FirstName,
                LastName = member.Person!.LastName,
                member.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new MemberSummary(
                row.Id,
                row.GymId,
                row.MemberCode,
                BuildFullName(row.FirstName, row.LastName),
                row.Status.ToString());
    }

    public async Task<MemberSummary?> FindMemberForUserAsync(
        Guid gymId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Members
            .AsNoTracking()
            .Where(member =>
                member.GymId == gymId &&
                member.Person != null &&
                member.Person.AppUser != null &&
                member.Person.AppUser.Id == userId)
            .Select(member => new
            {
                member.Id,
                member.GymId,
                member.MemberCode,
                FirstName = member.Person!.FirstName,
                LastName = member.Person!.LastName,
                member.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new MemberSummary(
                row.Id,
                row.GymId,
                row.MemberCode,
                BuildFullName(row.FirstName, row.LastName),
                row.Status.ToString());
    }

    private static string BuildFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();
}
