using App.BLL.Services;
using App.DTO.v1.Members;
using BuildingBlocks.Mediator;
using Modules.GymManagement.Contracts;

namespace Modules.GymManagement.Application.Members;

internal sealed class ListMembersQueryHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<ListMembersQuery, IReadOnlyCollection<MemberResponse>>
{
    public Task<IReadOnlyCollection<MemberResponse>> HandleAsync(ListMembersQuery request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.GetMembersAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class GetCurrentMemberQueryHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<GetCurrentMemberQuery, MemberDetailResponse>
{
    public Task<MemberDetailResponse> HandleAsync(GetCurrentMemberQuery request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.GetCurrentMemberAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class GetMemberQueryHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<GetMemberQuery, MemberDetailResponse>
{
    public Task<MemberDetailResponse> HandleAsync(GetMemberQuery request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.GetMemberAsync(request.GymCode, request.MemberId, cancellationToken);
    }
}

internal sealed class CreateMemberCommandHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<CreateMemberCommand, MemberDetailResponse>
{
    public Task<MemberDetailResponse> HandleAsync(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.CreateMemberAsync(request.GymCode, request.Request, cancellationToken);
    }
}

internal sealed class UpdateMemberCommandHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<UpdateMemberCommand, MemberDetailResponse>
{
    public Task<MemberDetailResponse> HandleAsync(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.UpdateMemberAsync(request.GymCode, request.MemberId, request.Request, cancellationToken);
    }
}

internal sealed class DeleteMemberCommandHandler(IMemberWorkflowService memberWorkflowService)
    : IRequestHandler<DeleteMemberCommand>
{
    public Task HandleAsync(DeleteMemberCommand request, CancellationToken cancellationToken)
    {
        return memberWorkflowService.DeleteMemberAsync(request.GymCode, request.MemberId, cancellationToken);
    }
}
