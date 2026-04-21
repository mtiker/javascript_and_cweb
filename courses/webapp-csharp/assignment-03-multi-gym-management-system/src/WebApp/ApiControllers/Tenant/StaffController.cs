using System.Globalization;
using App.BLL.Contracts;
using App.DTO.v1;
using App.DTO.v1.Tenant;
using App.Domain.Common;
using App.Domain.Entities;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class StaffController(
    App.DAL.EF.AppDbContext dbContext,
    IAuthorizationService authorizationService) : ApiControllerBase(dbContext)
{
    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyCollection<StaffResponse>>> GetStaff(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var staff = await DbContext.Staff
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

        return Ok(staff);
    }

    [HttpPost("staff")]
    public async Task<ActionResult<StaffResponse>> CreateStaff(string gymCode, [FromBody] StaffUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

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

        DbContext.Staff.Add(staff);
        await DbContext.SaveChangesAsync();

        return Ok(new StaffResponse
        {
            Id = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = $"{staff.Person!.FirstName} {staff.Person!.LastName}".Trim(),
            Status = staff.Status
        });
    }

    [HttpPut("staff/{id:guid}")]
    public async Task<ActionResult<StaffResponse>> UpdateStaff(string gymCode, Guid id, [FromBody] StaffUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var staff = await DbContext.Staff.Include(entity => entity.Person).FirstOrDefaultAsync(entity => entity.Id == id)
                    ?? throw new App.BLL.Exceptions.AppNotFoundException("Staff member was not found.");

        staff.StaffCode = request.StaffCode.Trim();
        staff.Status = request.Status;
        staff.Person!.FirstName = request.FirstName.Trim();
        staff.Person.LastName = request.LastName.Trim();

        await DbContext.SaveChangesAsync();

        return Ok(new StaffResponse
        {
            Id = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = $"{staff.Person.FirstName} {staff.Person.LastName}".Trim(),
            Status = staff.Status
        });
    }

    [HttpDelete("staff/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteStaff(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var staff = await DbContext.Staff.FirstOrDefaultAsync(entity => entity.Id == id)
                    ?? throw new App.BLL.Exceptions.AppNotFoundException("Staff member was not found.");

        DbContext.Staff.Remove(staff);
        await DbContext.SaveChangesAsync();
        return Ok(new Message("Staff member deleted."));
    }

    [HttpGet("job-roles")]
    public async Task<ActionResult<IReadOnlyCollection<JobRoleResponse>>> GetJobRoles(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        return Ok(await DbContext.JobRoles
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Code)
            .Select(entity => new JobRoleResponse
            {
                Id = entity.Id,
                Code = entity.Code,
                Title = Translate(entity.Title) ?? string.Empty,
                Description = Translate(entity.Description)
            })
            .ToArrayAsync());
    }

    [HttpPost("job-roles")]
    public async Task<ActionResult<JobRoleResponse>> CreateJobRole(string gymCode, [FromBody] JobRoleUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var role = new JobRole
        {
            GymId = gymId,
            Code = request.Code.Trim(),
            Title = ToLangStr(request.Title),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };

        DbContext.JobRoles.Add(role);
        await DbContext.SaveChangesAsync();

        return Ok(new JobRoleResponse
        {
            Id = role.Id,
            Code = role.Code,
            Title = Translate(role.Title) ?? string.Empty,
            Description = Translate(role.Description)
        });
    }

    [HttpPut("job-roles/{id:guid}")]
    public async Task<ActionResult<JobRoleResponse>> UpdateJobRole(string gymCode, Guid id, [FromBody] JobRoleUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var role = await DbContext.JobRoles.FirstOrDefaultAsync(entity => entity.Id == id)
                   ?? throw new App.BLL.Exceptions.AppNotFoundException("Job role was not found.");

        role.Code = request.Code.Trim();
        role.Title = ToLangStr(request.Title);
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await DbContext.SaveChangesAsync();

        return Ok(new JobRoleResponse
        {
            Id = role.Id,
            Code = role.Code,
            Title = Translate(role.Title) ?? string.Empty,
            Description = Translate(role.Description)
        });
    }

    [HttpDelete("job-roles/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteJobRole(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var role = await DbContext.JobRoles.FirstOrDefaultAsync(entity => entity.Id == id)
                   ?? throw new App.BLL.Exceptions.AppNotFoundException("Job role was not found.");
        DbContext.JobRoles.Remove(role);
        await DbContext.SaveChangesAsync();
        return Ok(new Message("Job role deleted."));
    }

    [HttpGet("contracts")]
    public async Task<ActionResult<IReadOnlyCollection<ContractResponse>>> GetContracts(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        return Ok(await DbContext.EmploymentContracts
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
            .ToArrayAsync());
    }

    [HttpPost("contracts")]
    public async Task<ActionResult<ContractResponse>> CreateContract(string gymCode, [FromBody] ContractUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
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

        DbContext.EmploymentContracts.Add(contract);
        await DbContext.SaveChangesAsync();
        return Ok(ToContractResponse(contract));
    }

    [HttpPut("contracts/{id:guid}")]
    public async Task<ActionResult<ContractResponse>> UpdateContract(string gymCode, Guid id, [FromBody] ContractUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var contract = await DbContext.EmploymentContracts.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new App.BLL.Exceptions.AppNotFoundException("Contract was not found.");

        contract.StaffId = request.StaffId;
        contract.PrimaryJobRoleId = request.PrimaryJobRoleId;
        contract.WorkloadPercent = request.WorkloadPercent;
        contract.JobDescription = string.IsNullOrWhiteSpace(request.JobDescription) ? null : ToLangStr(request.JobDescription);
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.ContractStatus = request.ContractStatus;
        contract.EmployerType = request.EmployerType;
        contract.EmployerName = request.EmployerName?.Trim();

        await DbContext.SaveChangesAsync();
        return Ok(ToContractResponse(contract));
    }

    [HttpDelete("contracts/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteContract(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var contract = await DbContext.EmploymentContracts.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new App.BLL.Exceptions.AppNotFoundException("Contract was not found.");
        DbContext.EmploymentContracts.Remove(contract);
        await DbContext.SaveChangesAsync();
        return Ok(new Message("Contract deleted."));
    }

    [HttpGet("vacations")]
    public async Task<ActionResult<IReadOnlyCollection<VacationResponse>>> GetVacations(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        return Ok(await DbContext.Vacations
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
            .ToArrayAsync());
    }

    [HttpPost("vacations")]
    public async Task<ActionResult<VacationResponse>> CreateVacation(string gymCode, [FromBody] VacationUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
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

        DbContext.Vacations.Add(vacation);
        await DbContext.SaveChangesAsync();
        return Ok(ToVacationResponse(vacation));
    }

    [HttpPut("vacations/{id:guid}")]
    public async Task<ActionResult<VacationResponse>> UpdateVacation(string gymCode, Guid id, [FromBody] VacationUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var vacation = await DbContext.Vacations.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new App.BLL.Exceptions.AppNotFoundException("Vacation was not found.");

        vacation.ContractId = request.ContractId;
        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        vacation.VacationType = request.VacationType;
        vacation.Status = request.Status;
        vacation.Comment = request.Comment?.Trim();

        await DbContext.SaveChangesAsync();
        return Ok(ToVacationResponse(vacation));
    }

    [HttpDelete("vacations/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteVacation(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var vacation = await DbContext.Vacations.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new App.BLL.Exceptions.AppNotFoundException("Vacation was not found.");
        DbContext.Vacations.Remove(vacation);
        await DbContext.SaveChangesAsync();
        return Ok(new Message("Vacation deleted."));
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
}
