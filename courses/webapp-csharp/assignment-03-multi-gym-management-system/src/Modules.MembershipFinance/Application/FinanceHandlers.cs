using App.BLL.Services;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using BuildingBlocks.Mediator;
using Modules.MembershipFinance.Contracts;

namespace Modules.MembershipFinance.Application;

internal sealed class ListMembershipsQueryHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<ListMembershipsQuery, IReadOnlyCollection<MembershipResponse>>
{
    public Task<IReadOnlyCollection<MembershipResponse>> HandleAsync(ListMembershipsQuery request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.GetMembershipsAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class SellMembershipCommandHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<SellMembershipCommand, MembershipSaleResponse>
{
    public Task<MembershipSaleResponse> HandleAsync(SellMembershipCommand request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.SellMembershipAsync(request.GymCode, request.Request, cancellationToken);
    }
}

internal sealed class UpdateMembershipStatusCommandHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<UpdateMembershipStatusCommand, MembershipResponse>
{
    public Task<MembershipResponse> HandleAsync(UpdateMembershipStatusCommand request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.UpdateMembershipStatusAsync(request.GymCode, request.MembershipId, request.Request, cancellationToken);
    }
}

internal sealed class DeleteMembershipCommandHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<DeleteMembershipCommand>
{
    public Task HandleAsync(DeleteMembershipCommand request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.DeleteMembershipAsync(request.GymCode, request.MembershipId, cancellationToken);
    }
}

internal sealed class ListPaymentsQueryHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<ListPaymentsQuery, IReadOnlyCollection<PaymentResponse>>
{
    public Task<IReadOnlyCollection<PaymentResponse>> HandleAsync(ListPaymentsQuery request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.GetPaymentsAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class CreatePaymentCommandHandler(IMembershipWorkflowService membershipWorkflowService)
    : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    public Task<PaymentResponse> HandleAsync(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        return membershipWorkflowService.CreatePaymentAsync(request.GymCode, request.Request, cancellationToken);
    }
}
