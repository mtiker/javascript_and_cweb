using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Identity;
using Shared.Contracts.Dtos.v1.Members;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class Final1CriticalE2ETests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task Login_E2E_ReturnsTenantSession()
    {
        var client = factory.CreateClient();

        var payload = await LoginAsync(client, "admin@peakforge.local");

        Assert.False(string.IsNullOrWhiteSpace(payload.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Equal(GymCode, payload.ActiveGymCode);
        Assert.Contains(payload.AvailableTenants, tenant => tenant.GymCode == GymCode);
        Assert.Contains(payload.AvailableTenants, tenant => tenant.Roles.Contains("GymAdmin"));
    }

    [Fact]
    public async Task MemberCrud_E2E_CreateReadUpdateDelete()
    {
        var client = await CreateAdminApiClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var memberCode = $"MEM-F1-{suffix}";
        var personalCode = "49" + Random.Shared.Next(100000000, 999999999).ToString();

        var createResponse = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", new MemberUpsertRequest
        {
            FirstName = "Final",
            LastName = "Member",
            MemberCode = memberCode,
            PersonalCode = personalCode,
            Status = MemberStatus.Active
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<MemberDetailResponse>())!;
        Assert.Equal(memberCode, created.MemberCode);

        var readResponse = await client.GetAsync($"/api/v1/{GymCode}/members/{created.Id}");
        readResponse.EnsureSuccessStatusCode();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/{GymCode}/members/{created.Id}", new MemberUpsertRequest
        {
            FirstName = "Final",
            LastName = "Updated",
            MemberCode = memberCode,
            PersonalCode = personalCode,
            Status = MemberStatus.Suspended
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<MemberDetailResponse>())!;
        Assert.Equal(MemberStatus.Suspended, updated.Status);

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/api/v1/{GymCode}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task TrainingCategoryCrud_E2E_CreateReadUpdateDelete()
    {
        var client = await CreateAdminApiClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"/api/v1/{GymCode}/training-categories", new TrainingCategoryUpsertRequest
        {
            Name = $"Final1 Strength {suffix}",
            Description = "Final1 E2E category"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse>())!;

        var listResponse = await client.GetAsync($"/api/v1/{GymCode}/training-categories");
        listResponse.EnsureSuccessStatusCode();
        var list = (await listResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>())!;
        Assert.Contains(list, category => category.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/{GymCode}/training-categories/{created.Id}", new TrainingCategoryUpsertRequest
        {
            Name = $"Final1 Conditioning {suffix}",
            Description = "Final1 E2E category updated"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse>())!;
        Assert.Equal($"Final1 Conditioning {suffix}", updated.Name);

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/training-categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task MembershipPackageCrud_E2E_CreateReadUpdateDelete()
    {
        var client = await CreateAdminApiClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"/api/v1/{GymCode}/membership-packages", NewPackageRequest($"Final1 Monthly {suffix}", 79m));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<MembershipPackageResponse>())!;

        var listResponse = await client.GetAsync($"/api/v1/{GymCode}/membership-packages");
        listResponse.EnsureSuccessStatusCode();
        var list = (await listResponse.Content.ReadFromJsonAsync<MembershipPackageResponse[]>())!;
        Assert.Contains(list, package => package.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/{GymCode}/membership-packages/{created.Id}",
            NewPackageRequest($"Final1 Monthly Pro {suffix}", 99m));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<MembershipPackageResponse>())!;
        Assert.Equal(99m, updated.BasePrice);

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/membership-packages/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task IdorNegative_E2E_CrossTenantMemberUpdateReturns404()
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

        var client = await CreateMultiGymAdminApiClientAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/north-star/members/{peakForgeMemberId}", new MemberUpsertRequest
        {
            FirstName = "Cross",
            LastName = "Tenant",
            MemberCode = "MEM-F1-IDOR",
            PersonalCode = "49999010101",
            Status = MemberStatus.Active
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    private async Task<HttpClient> CreateAdminApiClientAsync()
    {
        var client = factory.CreateClient();
        var payload = await LoginAsync(client, "admin@peakforge.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private async Task<HttpClient> CreateMultiGymAdminApiClientAsync()
    {
        var client = factory.CreateClient();
        var payload = await LoginAsync(client, "multigym.admin@gym.local");
        Assert.Equal("north-star", payload.ActiveGymCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private static async Task<JwtResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = "GymStrong123!"
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JwtResponse>())!;
    }

    private static MembershipPackageUpsertRequest NewPackageRequest(string name, decimal price) =>
        new()
        {
            Name = name,
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = price,
            CurrencyCode = "EUR",
            TrainingDiscountPercent = null,
            IsTrainingFree = false,
            Description = "Final1 E2E package"
        };
}
