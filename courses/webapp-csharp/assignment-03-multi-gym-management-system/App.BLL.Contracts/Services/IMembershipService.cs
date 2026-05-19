using App.DTO.v1.Memberships;

namespace App.BLL.Contracts.Services;

public interface IMembershipService
{
    Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MembershipAdminSummaryResponse>> GetActiveMembershipSummariesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
