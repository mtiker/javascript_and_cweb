using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Members;

namespace App.BLL.Services;

public interface IMemberWorkflowService
{
    Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode);
    Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode);
    Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id);
    Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request);
    Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request);
    Task DeleteMemberAsync(string gymCode, Guid id);
}
