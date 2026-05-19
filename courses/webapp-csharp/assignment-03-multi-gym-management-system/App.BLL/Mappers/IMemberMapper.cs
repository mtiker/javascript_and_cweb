using App.Domain.Entities;
using App.DTO.v1.Members;

namespace App.BLL.Mappers;

public interface IMemberMapper
{
    MemberResponse ToSummary(Member member);

    MemberDetailResponse ToDetail(Member member);

    IReadOnlyCollection<MemberResponse> ToSummaryList(IEnumerable<Member> members);
}
