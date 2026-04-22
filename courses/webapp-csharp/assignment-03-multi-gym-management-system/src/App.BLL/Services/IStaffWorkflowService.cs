using App.DTO.v1.EmploymentContracts;
using App.DTO.v1.JobRoles;
using App.DTO.v1.Staff;
using App.DTO.v1.Vacations;

namespace App.BLL.Services;

public interface IStaffWorkflowService
{
    Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode);
    Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request);
    Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request);
    Task DeleteStaffAsync(string gymCode, Guid id);

    Task<IReadOnlyCollection<JobRoleResponse>> GetJobRolesAsync(string gymCode);
    Task<JobRoleResponse> CreateJobRoleAsync(string gymCode, JobRoleUpsertRequest request);
    Task<JobRoleResponse> UpdateJobRoleAsync(string gymCode, Guid id, JobRoleUpsertRequest request);
    Task DeleteJobRoleAsync(string gymCode, Guid id);

    Task<IReadOnlyCollection<ContractResponse>> GetContractsAsync(string gymCode);
    Task<ContractResponse> CreateContractAsync(string gymCode, ContractUpsertRequest request);
    Task<ContractResponse> UpdateContractAsync(string gymCode, Guid id, ContractUpsertRequest request);
    Task DeleteContractAsync(string gymCode, Guid id);

    Task<IReadOnlyCollection<VacationResponse>> GetVacationsAsync(string gymCode);
    Task<VacationResponse> CreateVacationAsync(string gymCode, VacationUpsertRequest request);
    Task<VacationResponse> UpdateVacationAsync(string gymCode, Guid id, VacationUpsertRequest request);
    Task DeleteVacationAsync(string gymCode, Guid id);
}
