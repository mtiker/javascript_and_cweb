using App.DTO.v1.Staff;

namespace App.BLL.Services;

public interface IStaffWorkflowService
{
    Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
