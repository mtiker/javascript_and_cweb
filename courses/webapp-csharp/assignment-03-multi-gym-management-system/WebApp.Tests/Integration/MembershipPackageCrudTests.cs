using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.DTO.v1.Identity;
using App.DTO.v1.Members;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class MembershipPackageCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task GetMembershipPackages_ReturnsListForActiveGym()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/v1/{GymCode}/membership-packages");

        response.EnsureSuccessStatusCode();
        var packages = await response.Content.ReadFromJsonAsync<MembershipPackageResponse[]>();

        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
        Assert.All(packages, package =>
        {
            Assert.False(string.IsNullOrWhiteSpace(package.Name));
            Assert.False(string.IsNullOrWhiteSpace(package.CurrencyCode));
            Assert.True(package.DurationValue > 0);
            Assert.True(package.BasePrice >= 0);
        });
    }

    [Fact]
    public async Task CreateMembershipPackage_Returns201()
    {
        var client = await CreateAdminClientAsync();
        var request = NewPackageRequest("Phase 6 Monthly");

        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/membership-packages", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MembershipPackageResponse>();
        Assert.NotNull(created);
        Assert.Equal(request.Name, created!.Name);
        Assert.Equal(request.BasePrice, created.BasePrice);
        Assert.Equal("EUR", created.CurrencyCode);
    }

    [Fact]
    public async Task CreateMembershipPackage_InvalidPrice_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();
        var request = NewPackageRequest("Phase 6 Invalid Price");
        request.BasePrice = -1m;

        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/membership-packages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Base price", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMembershipPackage_InvalidDuration_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();
        var request = NewPackageRequest("Phase 6 Invalid Duration");
        request.DurationValue = 0;

        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/membership-packages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duration", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMembershipPackage_MissingCurrency_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/{GymCode}/membership-packages",
            new
            {
                name = "Phase 6 Missing Currency",
                packageType = 1,
                durationValue = 1,
                durationUnit = 1,
                basePrice = 59m,
                trainingDiscountPercent = (int?)null,
                isTrainingFree = false,
                description = "Currency intentionally omitted."
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Currency code", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateMembershipPackage_ReturnsUpdatedPackage()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreatePackageAsync(client, "Phase 6 Update");
        var request = NewPackageRequest("Phase 6 Updated");
        request.BasePrice = 89m;
        request.DurationValue = 2;
        request.TrainingDiscountPercent = 25;
        request.Description = "Updated package terms.";

        var response = await client.PutAsJsonAsync($"/api/v1/{GymCode}/membership-packages/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MembershipPackageResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Phase 6 Updated", updated.Name);
        Assert.Equal(89m, updated.BasePrice);
        Assert.Equal(2, updated.DurationValue);
        Assert.Equal(25, updated.TrainingDiscountPercent);
    }

    [Fact]
    public async Task DeleteUnusedMembershipPackage_SoftDeletesPackage()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreatePackageAsync(client, "Phase 6 Delete Unused");

        var response = await client.DeleteAsync($"/api/v1/{GymCode}/membership-packages/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedPackage = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .SingleAsync(package => package.Id == created.Id);
        Assert.True(deletedPackage.IsDeleted);
        Assert.NotNull(deletedPackage.DeletedAtUtc);
    }

    [Fact]
    public async Task DeleteUsedMembershipPackage_ReturnsConflictAndKeepsMembershipSnapshot()
    {
        var client = await CreateAdminClientAsync();
        var package = await CreatePackageAsync(client, "Phase 6 Delete Used");
        var member = await CreateMemberAsync(client, "MEM-PHASE6-PKG", "49906010101");

        var saleResponse = await client.PostAsJsonAsync($"/api/v1/{GymCode}/memberships", new SellMembershipRequest
        {
            MemberId = member.Id,
            MembershipPackageId = package.Id,
            RequestedStartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2)),
            PaymentReference = "PHASE6-PKG"
        });
        saleResponse.EnsureSuccessStatusCode();
        var sale = await saleResponse.Content.ReadFromJsonAsync<MembershipSaleResponse>();
        Assert.NotNull(sale);
        Assert.False(sale!.OverlapDetected);

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/membership-packages/{package.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", deleteResponse.Content.Headers.ContentType?.ToString());
        var body = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("deactivate", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var packageAfterDeleteAttempt = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.Id == package.Id);
        var membership = await dbContext.Memberships
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.Id == sale.MembershipId);
        Assert.False(packageAfterDeleteAttempt.IsDeleted);
        Assert.Equal(package.Id, membership.MembershipPackageId);
        Assert.Equal(package.BasePrice, membership.PriceAtPurchase);
        Assert.Equal(package.CurrencyCode, membership.CurrencyCode);
    }

    [Fact]
    public async Task UpdateMembershipPackage_ForeignGymPackageId_Returns404()
    {
        Guid peakForgePackageId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var peakForge = await dbContext.Gyms.SingleAsync(gym => gym.Code == GymCode);
            peakForgePackageId = await dbContext.MembershipPackages
                .Where(package => package.GymId == peakForge.Id)
                .Select(package => package.Id)
                .FirstAsync();
        }

        var client = await CreateMultiGymAdminClientAsync();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/north-star/membership-packages/{peakForgePackageId}",
            NewPackageRequest("North Star IDOR Attempt"));

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

    private static MembershipPackageUpsertRequest NewPackageRequest(string name)
    {
        return new MembershipPackageUpsertRequest
        {
            Name = name,
            PackageType = App.Domain.Enums.MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = App.Domain.Enums.DurationUnit.Month,
            BasePrice = 79m,
            CurrencyCode = "EUR",
            TrainingDiscountPercent = null,
            IsTrainingFree = false,
            Description = "Phase 6 package test."
        };
    }

    private static async Task<MembershipPackageResponse> CreatePackageAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/membership-packages", NewPackageRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MembershipPackageResponse>())!;
    }

    private static async Task<MemberDetailResponse> CreateMemberAsync(HttpClient client, string memberCode, string personalCode)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/members", new MemberUpsertRequest
        {
            FirstName = "Package",
            LastName = "Buyer",
            MemberCode = memberCode,
            PersonalCode = personalCode,
            Status = App.Domain.Enums.MemberStatus.Active
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MemberDetailResponse>())!;
    }
}
