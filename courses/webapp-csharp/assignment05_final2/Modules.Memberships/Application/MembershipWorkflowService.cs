using App.BLL.Contracts.Services;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Payments;

namespace Modules.Memberships.Application;

public class MembershipWorkflowService(
    IMembershipPackageService membershipPackageService,
    IMembershipService membershipService,
    IPaymentService paymentService) : IMembershipWorkflowService
{
    public Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        return membershipPackageService.GetPackagesAsync(gymCode, cancellationToken);
    }

    public Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return membershipPackageService.CreatePackageAsync(gymCode, request, cancellationToken);
    }

    public Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return membershipPackageService.UpdatePackageAsync(gymCode, id, request, cancellationToken);
    }

    public Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        return membershipPackageService.DeletePackageAsync(gymCode, id, cancellationToken);
    }

    public Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, MembershipFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return membershipService.GetMembershipsAsync(gymCode, filter, cancellationToken);
    }

    public Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        return membershipService.SellMembershipAsync(gymCode, request, cancellationToken);
    }

    public Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return membershipService.UpdateMembershipStatusAsync(gymCode, id, request, cancellationToken);
    }

    public Task<MembershipResponse> UpdateMembershipAsync(string gymCode, Guid id, MembershipEditRequest request, CancellationToken cancellationToken = default)
    {
        return membershipService.UpdateMembershipAsync(gymCode, id, request, cancellationToken);
    }

    public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        return membershipService.DeleteMembershipAsync(gymCode, id, cancellationToken);
    }

    public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, PaymentFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return paymentService.GetPaymentsAsync(gymCode, filter, cancellationToken);
    }

    public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default)
    {
        return paymentService.CreatePaymentAsync(gymCode, request, cancellationToken);
    }

    public Task<PaymentResponse> RefundPaymentAsync(string gymCode, Guid paymentId, PaymentRefundRequest request, CancellationToken cancellationToken = default)
    {
        return paymentService.RefundPaymentAsync(gymCode, paymentId, request, cancellationToken);
    }

}
