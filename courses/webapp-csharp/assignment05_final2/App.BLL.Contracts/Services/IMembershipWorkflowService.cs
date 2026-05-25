using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Payments;

namespace App.BLL.Contracts.Services;

public interface IMembershipWorkflowService
{
    Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default);
    Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, MembershipFilter? filter = null, CancellationToken cancellationToken = default);
    Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse> UpdateMembershipAsync(string gymCode, Guid id, MembershipEditRequest request, CancellationToken cancellationToken = default);
    Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, PaymentFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundPaymentAsync(string gymCode, Guid paymentId, PaymentRefundRequest request, CancellationToken cancellationToken = default);
}
