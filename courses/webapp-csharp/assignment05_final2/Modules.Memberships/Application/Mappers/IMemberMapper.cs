using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.Members;

namespace Modules.Memberships.Application.Mappers;

public interface IMemberMapper
{
    MemberResponse ToSummary(Member member);

    MemberDetailResponse ToDetail(Member member);

    IReadOnlyCollection<MemberResponse> ToSummaryList(IEnumerable<Member> members);
}
