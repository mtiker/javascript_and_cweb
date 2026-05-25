using Shared.Contracts.Dtos.v1.Staff;

namespace App.BLL.Contracts.Services;

public interface IStaffWorkflowService
{
    Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, StaffFilter? filter = null, CancellationToken cancellationToken = default);
    Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffResponse> UpdateStaffStatusAsync(string gymCode, Guid id, StaffStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
