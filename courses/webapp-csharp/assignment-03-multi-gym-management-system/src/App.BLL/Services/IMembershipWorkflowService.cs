using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Services;

public interface IMembershipWorkflowService
{
    Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode);
    Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request);
    Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request);
    Task DeletePackageAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode);
    Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request);
    Task DeleteMembershipAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode);
    Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request);
    Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession);
}
