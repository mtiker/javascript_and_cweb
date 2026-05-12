using App.BLL.Services;
using App.DTO.v1.Finance;
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

internal sealed class GetCurrentFinanceWorkspaceQueryHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<GetCurrentFinanceWorkspaceQuery, FinanceWorkspaceResponse>
{
    public Task<FinanceWorkspaceResponse> HandleAsync(GetCurrentFinanceWorkspaceQuery request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.GetCurrentWorkspaceAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class GetMemberFinanceWorkspaceQueryHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<GetMemberFinanceWorkspaceQuery, FinanceWorkspaceResponse>
{
    public Task<FinanceWorkspaceResponse> HandleAsync(GetMemberFinanceWorkspaceQuery request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.GetWorkspaceAsync(request.GymCode, request.MemberId, cancellationToken);
    }
}

internal sealed class ListInvoicesQueryHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<ListInvoicesQuery, IReadOnlyCollection<InvoiceResponse>>
{
    public Task<IReadOnlyCollection<InvoiceResponse>> HandleAsync(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.GetInvoicesAsync(request.GymCode, request.MemberId, cancellationToken);
    }
}

internal sealed class GetInvoiceQueryHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<GetInvoiceQuery, InvoiceResponse>
{
    public Task<InvoiceResponse> HandleAsync(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.GetInvoiceAsync(request.GymCode, request.InvoiceId, cancellationToken);
    }
}

internal sealed class CreateInvoiceCommandHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<CreateInvoiceCommand, InvoiceResponse>
{
    public Task<InvoiceResponse> HandleAsync(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.CreateInvoiceAsync(request.GymCode, request.Request, cancellationToken);
    }
}

internal sealed class PostInvoicePaymentCommandHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<PostInvoicePaymentCommand, InvoiceResponse>
{
    public Task<InvoiceResponse> HandleAsync(PostInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.AddInvoicePaymentAsync(request.GymCode, request.InvoiceId, request.Request, cancellationToken);
    }
}

internal sealed class PostInvoiceRefundCommandHandler(IFinanceWorkspaceService financeWorkspaceService)
    : IRequestHandler<PostInvoiceRefundCommand, InvoiceResponse>
{
    public Task<InvoiceResponse> HandleAsync(PostInvoiceRefundCommand request, CancellationToken cancellationToken)
    {
        return financeWorkspaceService.AddInvoiceRefundAsync(request.GymCode, request.InvoiceId, request.Request, cancellationToken);
    }
}
