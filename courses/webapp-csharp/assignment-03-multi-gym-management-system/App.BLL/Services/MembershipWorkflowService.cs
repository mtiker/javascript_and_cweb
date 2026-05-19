using App.Domain.Entities;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Services;

public class MembershipWorkflowService(
    IMembershipPackageService membershipPackageService,
    IMembershipService membershipService,
    IPaymentService paymentService,
    IBookingPricingService bookingPricingService) : IMembershipWorkflowService
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

    public Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        return membershipService.GetMembershipsAsync(gymCode, cancellationToken);
    }

    public Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        return membershipService.SellMembershipAsync(gymCode, request, cancellationToken);
    }

    public Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return membershipService.UpdateMembershipStatusAsync(gymCode, id, request, cancellationToken);
    }

    public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        return membershipService.DeleteMembershipAsync(gymCode, id, cancellationToken);
    }

    public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        return paymentService.GetPaymentsAsync(gymCode, cancellationToken);
    }

    public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default)
    {
        return paymentService.CreatePaymentAsync(gymCode, request, cancellationToken);
    }

    public Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default)
    {
        return bookingPricingService.CalculateBookingPriceAsync(gymId, memberId, trainingSession, cancellationToken);
    }
}
