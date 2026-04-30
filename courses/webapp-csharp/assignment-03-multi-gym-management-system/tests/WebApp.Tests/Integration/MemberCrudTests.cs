using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.DTO.v1.Identity;
using App.DTO.v1.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class MemberCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task GetMembers_ReturnsListForActiveGym()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/v1/{GymCode}/members");

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<MemberResponse[]>();

        Assert.NotNull(members);
        Assert.NotEmpty(members);
        Assert.Contains(members, m => m.MemberCode == "MEM-001");
        Assert.All(members, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.MemberCode));
            Assert.False(string.IsNullOrWhiteSpace(m.FullName));
        });
    }

    [Fact]
    public async Task GetMember_ReturnsDetail_ForGymAdmin()
    {
        var client = await CreateAdminClientAsync();

        var listResponse = await client.GetAsync($"/api/v1/{GymCode}/members");
        listResponse.EnsureSuccessStatusCode();
        var members = (await listResponse.Content.ReadFromJsonAsync<MemberResponse[]>())!;
        var existing = members.First(m => m.MemberCode == "MEM-001");

        var response = await client.GetAsync($"/api/v1/{GymCode}/members/{existing.Id}");

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();

        Assert.NotNull(detail);
        Assert.Equal(existing.Id, detail!.Id);
        Assert.Equal("MEM-001", detail.MemberCode);
        Assert.False(string.IsNullOrWhiteSpace(detail.FirstName));
        Assert.False(string.IsNullOrWhiteSpace(detail.LastName));
        Assert.Equal($"{detail.FirstName} {detail.LastName}".Trim(), detail.FullName);
    }

    [Fact]
    public async Task CreateMember_Returns201_AndLocationHeader()
    {
        var client = await CreateAdminClientAsync();
        var request = NewMemberRequest("MEM-PHASE4-201", "49901010101");

        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var detail = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(request.MemberCode, detail!.MemberCode);
        Assert.Equal(request.FirstName, detail.FirstName);
        Assert.Equal(request.LastName, detail.LastName);
        Assert.Contains($"/api/v1/{GymCode}/members/{detail.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task UpdateMember_Returns200_AndUpdatedDetail()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateMemberAsync(client, "MEM-PHASE4-PUT", "49901020102");

        var updated = new MemberUpsertRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            MemberCode = created.MemberCode,
            PersonalCode = "49901020102",
            Status = App.Domain.Enums.MemberStatus.Suspended
        };

        var response = await client.PutAsJsonAsync($"/api/v1/{GymCode}/members/{created.Id}", updated);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal("Updated", detail.FirstName);
        Assert.Equal("Name", detail.LastName);
        Assert.Equal(App.Domain.Enums.MemberStatus.Suspended, detail.Status);
    }

    [Fact]
    public async Task DeleteMember_Returns204_AndSubsequentGetIs404()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateMemberAsync(client, "MEM-PHASE4-DEL", "49901030103");

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var followUp = await client.GetAsync($"/api/v1/{GymCode}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, followUp.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", followUp.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task DeleteMember_SoftDeletesMemberRow()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateMemberAsync(client, "MEM-PHASE4-SOFT", "49901030104");

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/members/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedMember = await dbContext.Members
            .IgnoreQueryFilters()
            .SingleAsync(member => member.Id == created.Id);
        Assert.True(deletedMember.IsDeleted);
        Assert.NotNull(deletedMember.DeletedAtUtc);
    }

    [Fact]
    public async Task CreateMember_DuplicateMemberCode_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();
        await CreateMemberAsync(client, "MEM-PHASE4-DUP-CODE", "49901040104");

        var duplicate = NewMemberRequest("MEM-PHASE4-DUP-CODE", "49901040105");
        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", duplicate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Member code already exists", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMember_DuplicatePersonalCode_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();
        await CreateMemberAsync(client, "MEM-PHASE4-DUP-P-A", "49901050105");

        var duplicate = NewMemberRequest("MEM-PHASE4-DUP-P-B", "49901050105");
        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", duplicate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Personal code", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateMember_ForeignGymMemberId_Returns404()
    {
        Guid peakForgeMemberId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var peakForge = await dbContext.Gyms.SingleAsync(gym => gym.Code == GymCode);
            peakForgeMemberId = await dbContext.Members
                .Where(member => member.GymId == peakForge.Id && member.MemberCode == "MEM-001")
                .Select(member => member.Id)
                .SingleAsync();
        }

        var client = await CreateMultiGymAdminClientAsync();
        var response = await client.PutAsJsonAsync("/api/v1/north-star/members/" + peakForgeMemberId, NewMemberRequest("MEM-NS-001", "49901060106"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "admin@peakforge.local",
            Password = "GymStrong123!"
        });
        login.EnsureSuccessStatusCode();
        var payload = (await login.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private async Task<HttpClient> CreateMultiGymAdminClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "multigym.admin@gym.local",
            Password = "GymStrong123!"
        });
        login.EnsureSuccessStatusCode();
        var payload = (await login.Content.ReadFromJsonAsync<JwtResponse>())!;
        Assert.Equal("north-star", payload.ActiveGymCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private static MemberUpsertRequest NewMemberRequest(string memberCode, string personalCode)
    {
        return new MemberUpsertRequest
        {
            FirstName = "Phase4",
            LastName = "Tester",
            MemberCode = memberCode,
            PersonalCode = personalCode,
            Status = App.Domain.Enums.MemberStatus.Active
        };
    }

    private static async Task<MemberDetailResponse> CreateMemberAsync(HttpClient client, string memberCode, string personalCode)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", NewMemberRequest(memberCode, personalCode));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MemberDetailResponse>())!;
    }
}
