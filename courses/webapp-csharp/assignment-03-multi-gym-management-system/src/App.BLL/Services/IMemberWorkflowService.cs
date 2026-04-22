using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Members;

namespace App.BLL.Services;

public interface IMemberWorkflowService
{
    Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request, CancellationToken cancellationToken = default);
    Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
