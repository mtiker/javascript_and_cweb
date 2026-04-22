using System.Globalization;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.EmploymentContracts;
using App.DTO.v1.JobRoles;
using App.DTO.v1.Staff;
using App.DTO.v1.Vacations;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class StaffWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IStaffWorkflowService
{
    public async Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        return await dbContext.Staff
            .Include(entity => entity.Person)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Person!.LastName)
            .ThenBy(entity => entity.Person!.FirstName)
            .Select(entity => new StaffResponse
            {
                Id = entity.Id,
                StaffCode = entity.StaffCode,
                FullName = $"{entity.Person!.FirstName} {entity.Person!.LastName}".Trim(),
                Status = entity.Status
            })
            .ToArrayAsync();
    }

    public async Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var staff = new Staff
        {
            GymId = gymId,
            StaffCode = request.StaffCode.Trim(),
            Status = request.Status,
            Person = new Person
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim()
            }
        };

        dbContext.Staff.Add(staff);
        await dbContext.SaveChangesAsync();
        return ToStaffResponse(staff);
    }

    public async Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var staff = await dbContext.Staff
                        .Include(entity => entity.Person)
                        .FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                    ?? throw new NotFoundException("Staff member was not found.");

        staff.StaffCode = request.StaffCode.Trim();
        staff.Status = request.Status;
        staff.Person!.FirstName = request.FirstName.Trim();
        staff.Person.LastName = request.LastName.Trim();

        await dbContext.SaveChangesAsync();
        return ToStaffResponse(staff);
    }

    public async Task DeleteStaffAsync(string gymCode, Guid id)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var staff = await dbContext.Staff.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                    ?? throw new NotFoundException("Staff member was not found.");

        dbContext.Staff.Remove(staff);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<JobRoleResponse>> GetJobRolesAsync(string gymCode)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        return await dbContext.JobRoles
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Code)
            .Select(entity => new JobRoleResponse
            {
                Id = entity.Id,
                Code = entity.Code,
                Title = Translate(entity.Title) ?? string.Empty,
                Description = Translate(entity.Description)
            })
            .ToArrayAsync();
    }

    public async Task<JobRoleResponse> CreateJobRoleAsync(string gymCode, JobRoleUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var role = new JobRole
        {
            GymId = gymId,
            Code = request.Code.Trim(),
            Title = ToLangStr(request.Title),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };

        dbContext.JobRoles.Add(role);
        await dbContext.SaveChangesAsync();
        return ToJobRoleResponse(role);
    }

    public async Task<JobRoleResponse> UpdateJobRoleAsync(string gymCode, Guid id, JobRoleUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var role = await dbContext.JobRoles.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                   ?? throw new NotFoundException("Job role was not found.");

        role.Code = request.Code.Trim();
        role.Title = ToLangStr(request.Title);
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await dbContext.SaveChangesAsync();
        return ToJobRoleResponse(role);
    }

    public async Task DeleteJobRoleAsync(string gymCode, Guid id)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var role = await dbContext.JobRoles.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                   ?? throw new NotFoundException("Job role was not found.");

        dbContext.JobRoles.Remove(role);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<ContractResponse>> GetContractsAsync(string gymCode)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        return await dbContext.EmploymentContracts
            .Where(entity => entity.GymId == gymId)
            .OrderByDescending(entity => entity.StartDate)
            .Select(entity => new ContractResponse
            {
                Id = entity.Id,
                StaffId = entity.StaffId,
                PrimaryJobRoleId = entity.PrimaryJobRoleId,
                WorkloadPercent = entity.WorkloadPercent,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                ContractStatus = entity.ContractStatus,
                EmployerType = entity.EmployerType
            })
            .ToArrayAsync();
    }

    public async Task<ContractResponse> CreateContractAsync(string gymCode, ContractUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        await EnsureStaffAndRoleBelongToGymAsync(gymId, request.StaffId, request.PrimaryJobRoleId);

        var contract = new EmploymentContract
        {
            GymId = gymId,
            StaffId = request.StaffId,
            PrimaryJobRoleId = request.PrimaryJobRoleId,
            WorkloadPercent = request.WorkloadPercent,
            JobDescription = string.IsNullOrWhiteSpace(request.JobDescription) ? null : ToLangStr(request.JobDescription),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractStatus = request.ContractStatus,
            EmployerType = request.EmployerType,
            EmployerName = request.EmployerName?.Trim()
        };

        dbContext.EmploymentContracts.Add(contract);
        await dbContext.SaveChangesAsync();
        return ToContractResponse(contract);
    }

    public async Task<ContractResponse> UpdateContractAsync(string gymCode, Guid id, ContractUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        await EnsureStaffAndRoleBelongToGymAsync(gymId, request.StaffId, request.PrimaryJobRoleId);

        var contract = await dbContext.EmploymentContracts.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                       ?? throw new NotFoundException("Contract was not found.");

        contract.StaffId = request.StaffId;
        contract.PrimaryJobRoleId = request.PrimaryJobRoleId;
        contract.WorkloadPercent = request.WorkloadPercent;
        contract.JobDescription = string.IsNullOrWhiteSpace(request.JobDescription) ? null : ToLangStr(request.JobDescription);
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.ContractStatus = request.ContractStatus;
        contract.EmployerType = request.EmployerType;
        contract.EmployerName = request.EmployerName?.Trim();

        await dbContext.SaveChangesAsync();
        return ToContractResponse(contract);
    }

    public async Task DeleteContractAsync(string gymCode, Guid id)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var contract = await dbContext.EmploymentContracts.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                       ?? throw new NotFoundException("Contract was not found.");

        dbContext.EmploymentContracts.Remove(contract);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<VacationResponse>> GetVacationsAsync(string gymCode)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        return await dbContext.Vacations
            .Where(entity => entity.GymId == gymId)
            .OrderByDescending(entity => entity.StartDate)
            .Select(entity => new VacationResponse
            {
                Id = entity.Id,
                ContractId = entity.ContractId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                VacationType = entity.VacationType,
                Status = entity.Status,
                Comment = entity.Comment
            })
            .ToArrayAsync();
    }

    public async Task<VacationResponse> CreateVacationAsync(string gymCode, VacationUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        await EnsureContractBelongsToGymAsync(gymId, request.ContractId);

        var vacation = new Vacation
        {
            GymId = gymId,
            ContractId = request.ContractId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            VacationType = request.VacationType,
            Status = request.Status,
            Comment = request.Comment?.Trim()
        };

        dbContext.Vacations.Add(vacation);
        await dbContext.SaveChangesAsync();
        return ToVacationResponse(vacation);
    }

    public async Task<VacationResponse> UpdateVacationAsync(string gymCode, Guid id, VacationUpsertRequest request)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        await EnsureContractBelongsToGymAsync(gymId, request.ContractId);

        var vacation = await dbContext.Vacations.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                       ?? throw new NotFoundException("Vacation was not found.");

        vacation.ContractId = request.ContractId;
        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        vacation.VacationType = request.VacationType;
        vacation.Status = request.Status;
        vacation.Comment = request.Comment?.Trim();

        await dbContext.SaveChangesAsync();
        return ToVacationResponse(vacation);
    }

    public async Task DeleteVacationAsync(string gymCode, Guid id)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode);
        var vacation = await dbContext.Vacations.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId)
                       ?? throw new NotFoundException("Vacation was not found.");

        dbContext.Vacations.Remove(vacation);
        await dbContext.SaveChangesAsync();
    }

    private Task<Guid> EnsureStaffAdminAccessAsync(string gymCode)
    {
        return authorizationService.EnsureTenantAccessAsync(gymCode, RoleNames.GymOwner, RoleNames.GymAdmin);
    }

    private async Task EnsureStaffAndRoleBelongToGymAsync(Guid gymId, Guid staffId, Guid jobRoleId)
    {
        if (!await dbContext.Staff.AnyAsync(entity => entity.Id == staffId && entity.GymId == gymId))
        {
            throw new ValidationAppException("Staff member was not found in the active gym.");
        }

        if (!await dbContext.JobRoles.AnyAsync(entity => entity.Id == jobRoleId && entity.GymId == gymId))
        {
            throw new ValidationAppException("Job role was not found in the active gym.");
        }
    }

    private async Task EnsureContractBelongsToGymAsync(Guid gymId, Guid contractId)
    {
        if (!await dbContext.EmploymentContracts.AnyAsync(entity => entity.Id == contractId && entity.GymId == gymId))
        {
            throw new ValidationAppException("Contract was not found in the active gym.");
        }
    }

    private static StaffResponse ToStaffResponse(Staff staff)
    {
        return new StaffResponse
        {
            Id = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = $"{staff.Person!.FirstName} {staff.Person.LastName}".Trim(),
            Status = staff.Status
        };
    }

    private static JobRoleResponse ToJobRoleResponse(JobRole role)
    {
        return new JobRoleResponse
        {
            Id = role.Id,
            Code = role.Code,
            Title = Translate(role.Title) ?? string.Empty,
            Description = Translate(role.Description)
        };
    }

    private static ContractResponse ToContractResponse(EmploymentContract contract)
    {
        return new ContractResponse
        {
            Id = contract.Id,
            StaffId = contract.StaffId,
            PrimaryJobRoleId = contract.PrimaryJobRoleId,
            WorkloadPercent = contract.WorkloadPercent,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            ContractStatus = contract.ContractStatus,
            EmployerType = contract.EmployerType
        };
    }

    private static VacationResponse ToVacationResponse(Vacation vacation)
    {
        return new VacationResponse
        {
            Id = vacation.Id,
            ContractId = vacation.ContractId,
            StartDate = vacation.StartDate,
            EndDate = vacation.EndDate,
            VacationType = vacation.VacationType,
            Status = vacation.Status,
            Comment = vacation.Comment
        };
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
