using App.DTO.v1.MemberWorkspace;

namespace App.BLL.Services;

public interface IMemberWorkspaceService
{
    Task<MemberWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MemberWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);
}
