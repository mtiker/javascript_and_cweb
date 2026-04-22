using App.DTO.v1.EmploymentContracts;
using App.DTO.v1.JobRoles;
using App.DTO.v1.Staff;
using App.DTO.v1.Vacations;

namespace App.BLL.Services;

public interface IStaffWorkflowService
{
    Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JobRoleResponse>> GetJobRolesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<JobRoleResponse> CreateJobRoleAsync(string gymCode, JobRoleUpsertRequest request, CancellationToken cancellationToken = default);
    Task<JobRoleResponse> UpdateJobRoleAsync(string gymCode, Guid id, JobRoleUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteJobRoleAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContractResponse>> GetContractsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<ContractResponse> CreateContractAsync(string gymCode, ContractUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ContractResponse> UpdateContractAsync(string gymCode, Guid id, ContractUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteContractAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VacationResponse>> GetVacationsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<VacationResponse> CreateVacationAsync(string gymCode, VacationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<VacationResponse> UpdateVacationAsync(string gymCode, Guid id, VacationUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteVacationAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
