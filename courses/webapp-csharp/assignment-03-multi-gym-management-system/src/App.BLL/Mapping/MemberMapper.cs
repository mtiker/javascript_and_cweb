using App.Domain.Entities;
using App.DTO.v1.Members;

namespace App.BLL.Mapping;

public sealed class MemberMapper : IMemberMapper
{
    public MemberResponse ToSummary(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return new MemberResponse
        {
            Id = member.Id,
            MemberCode = member.MemberCode,
            FullName = BuildFullName(member.Person?.FirstName, member.Person?.LastName),
            Status = member.Status
        };
    }

    public MemberDetailResponse ToDetail(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var firstName = member.Person?.FirstName ?? string.Empty;
        var lastName = member.Person?.LastName ?? string.Empty;
        return new MemberDetailResponse
        {
            Id = member.Id,
            MemberCode = member.MemberCode,
            FirstName = firstName,
            LastName = lastName,
            FullName = BuildFullName(firstName, lastName),
            PersonalCode = member.Person?.PersonalCode,
            DateOfBirth = member.Person?.DateOfBirth,
            Status = member.Status
        };
    }

    public IReadOnlyCollection<MemberResponse> ToSummaryList(IEnumerable<Member> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        return members.Select(ToSummary).ToArray();
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        return $"{firstName ?? string.Empty} {lastName ?? string.Empty}".Trim();
    }
}
