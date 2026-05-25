using System.Security.Claims;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using App.Domain.Identity;
using Shared.Contracts.Dtos.v1.Members;

namespace App.BLL.Contracts.Services;

public interface IMemberWorkflowService
{
    Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode, MemberFilter? filter = null, CancellationToken cancellationToken = default);

    Task<MemberDetailResponse> UpdateMemberStatusAsync(string gymCode, Guid id, MemberStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
