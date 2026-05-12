using App.DTO.v1.Members;
using BuildingBlocks.Mediator;

namespace Modules.GymManagement.Contracts;

public sealed record ListMembersQuery(string GymCode) : IRequest<IReadOnlyCollection<MemberResponse>>;

public sealed record GetCurrentMemberQuery(string GymCode) : IRequest<MemberDetailResponse>;

public sealed record GetMemberQuery(string GymCode, Guid MemberId) : IRequest<MemberDetailResponse>;

public sealed record CreateMemberCommand(string GymCode, MemberUpsertRequest Request) : IRequest<MemberDetailResponse>;

public sealed record UpdateMemberCommand(string GymCode, Guid MemberId, MemberUpsertRequest Request) : IRequest<MemberDetailResponse>;

public sealed record DeleteMemberCommand(string GymCode, Guid MemberId) : IRequest;
