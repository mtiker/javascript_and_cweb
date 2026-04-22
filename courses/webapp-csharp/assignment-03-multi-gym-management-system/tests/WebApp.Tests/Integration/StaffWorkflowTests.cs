using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.EmploymentContracts;
using App.DTO.v1.Identity;
using App.DTO.v1.JobRoles;
using App.DTO.v1.Staff;
using App.DTO.v1.Vacations;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class StaffWorkflowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task StaffRelatedTenantEndpoints_SupportCrudThroughBllServices()
    {
        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var staffResponse = await client.PostAsJsonAsync("/api/v1/peak-forge/staff", new StaffUpsertRequest
        {
            FirstName = "Service",
            LastName = "Staff",
            StaffCode = $"STF-{suffix}",
            Status = StaffStatus.Active
        });
        staffResponse.EnsureSuccessStatusCode();
        var staff = (await staffResponse.Content.ReadFromJsonAsync<StaffResponse>())!;
        Assert.Equal("Service Staff", staff.FullName);

        var roleResponse = await client.PostAsJsonAsync("/api/v1/peak-forge/job-roles", new JobRoleUpsertRequest
        {
            Code = $"ROLE-{suffix}",
            Title = "Service Role",
            Description = "Created through staff workflow service"
        });
        roleResponse.EnsureSuccessStatusCode();
        var role = (await roleResponse.Content.ReadFromJsonAsync<JobRoleResponse>())!;
        Assert.Equal("Service Role", role.Title);

        var contractResponse = await client.PostAsJsonAsync("/api/v1/peak-forge/contracts", new ContractUpsertRequest
        {
            StaffId = staff.Id,
            PrimaryJobRoleId = role.Id,
            WorkloadPercent = 75m,
            JobDescription = "Service contract",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ContractStatus = ContractStatus.Active,
            EmployerType = EmployerType.Internal
        });
        contractResponse.EnsureSuccessStatusCode();
        var contract = (await contractResponse.Content.ReadFromJsonAsync<ContractResponse>())!;
        Assert.Equal(staff.Id, contract.StaffId);
        Assert.Equal(role.Id, contract.PrimaryJobRoleId);

        var vacationResponse = await client.PostAsJsonAsync("/api/v1/peak-forge/vacations", new VacationUpsertRequest
        {
            ContractId = contract.Id,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(20)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(24)),
            VacationType = VacationType.Annual,
            Status = VacationStatus.Planned,
            Comment = "Service vacation"
        });
        vacationResponse.EnsureSuccessStatusCode();
        var vacation = (await vacationResponse.Content.ReadFromJsonAsync<VacationResponse>())!;
        Assert.Equal(contract.Id, vacation.ContractId);

        var updatedStaffResponse = await client.PutAsJsonAsync($"/api/v1/peak-forge/staff/{staff.Id}", new StaffUpsertRequest
        {
            FirstName = "Updated",
            LastName = "Staff",
            StaffCode = $"STF-UP-{suffix}",
            Status = StaffStatus.Inactive
        });
        updatedStaffResponse.EnsureSuccessStatusCode();
        var updatedStaff = (await updatedStaffResponse.Content.ReadFromJsonAsync<StaffResponse>())!;
        Assert.Equal("Updated Staff", updatedStaff.FullName);
        Assert.Equal(StaffStatus.Inactive, updatedStaff.Status);

        (await client.DeleteAsync($"/api/v1/peak-forge/vacations/{vacation.Id}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/v1/peak-forge/contracts/{contract.Id}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/v1/peak-forge/job-roles/{role.Id}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/v1/peak-forge/staff/{staff.Id}")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task StaffRelatedTenantEndpoints_ReturnProblemDetailsForMissingResources()
    {
        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");

        var response = await client.DeleteAsync($"/api/v1/peak-forge/staff/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task StaffRelatedTenantEndpoints_RejectWrongActiveGym()
    {
        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");

        var response = await client.GetAsync("/api/v1/north-star/staff");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContractCreate_RejectsStaffFromAnotherGym()
    {
        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");
        var (northStarStaffId, peakForgeRoleId) = await GetCrossGymContractIdsAsync();

        var response = await client.PostAsJsonAsync("/api/v1/peak-forge/contracts", new ContractUpsertRequest
        {
            StaffId = northStarStaffId,
            PrimaryJobRoleId = peakForgeRoleId,
            WorkloadPercent = 50m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ContractStatus = ContractStatus.Active,
            EmployerType = EmployerType.Internal
        });
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Staff member was not found in the active gym.", content);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = "Gym123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);
        return client;
    }

    private async Task<(Guid NorthStarStaffId, Guid PeakForgeRoleId)> GetCrossGymContractIdsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var northStar = dbContext.Gyms.Single(entity => entity.Code == "north-star");
        var peakForge = dbContext.Gyms.Single(entity => entity.Code == "peak-forge");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var northStarStaff = new Staff
        {
            GymId = northStar.Id,
            StaffCode = $"NS-{suffix}",
            Person = new Person
            {
                FirstName = "North",
                LastName = "Staff"
            },
            Status = StaffStatus.Active
        };
        dbContext.Staff.Add(northStarStaff);

        var peakForgeRole = dbContext.JobRoles.FirstOrDefault(entity => entity.GymId == peakForge.Id);
        if (peakForgeRole == null)
        {
            peakForgeRole = new JobRole
            {
                GymId = peakForge.Id,
                Code = $"PF-{suffix}",
                Title = new LangStr("Peak Forge Role", "en")
            };
            dbContext.JobRoles.Add(peakForgeRole);
        }

        await dbContext.SaveChangesAsync();

        return (northStarStaff.Id, peakForgeRole.Id);
    }
}
