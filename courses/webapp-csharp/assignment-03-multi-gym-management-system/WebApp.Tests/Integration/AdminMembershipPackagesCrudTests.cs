using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Failing-by-design contract tests for the Admin/MembershipPackages MVC CRUD workflow.
// The Admin area currently exposes /Admin/Memberships (list-only); these tests pin
// the expected shape of a dedicated /Admin/MembershipPackages controller with full
// CRUD (GET form + POST handler), tenant isolation, and authorization gating.
public class AdminMembershipPackagesCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string MultiGymAdminEmail = "multigym.admin@gym.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminMembershipPackages_Index_AuthorizedAdminCanOpen()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/MembershipPackages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Seeded peak-forge packages: "Monthly Unlimited", "Day Pass", "Annual Performance".
        Assert.Contains("Monthly Unlimited", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminMembershipPackages_Create_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/MembershipPackages/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", html);
        Assert.Contains("BasePrice", html);
        Assert.Contains("DurationValue", html);
        Assert.Contains("CurrencyCode", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminMembershipPackages_Create_InvalidPost_ReturnsValidationErrors()
    {
        var client = await CreateMvcClientAsync(AdminEmail);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/MembershipPackages/Create");

        var response = await client.PostAsync("/Admin/MembershipPackages/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "",
            ["PackageType"] = nameof(MembershipPackageType.Monthly),
            ["DurationValue"] = "0",
            ["DurationUnit"] = nameof(DurationUnit.Month),
            ["BasePrice"] = "-1",
            ["CurrencyCode"] = "",
            ["IsTrainingFree"] = "false"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches(
            @"(field-validation-error|input-validation-error|validation-summary-errors)",
            html);
    }

    [Fact]
    public async Task AdminMembershipPackages_Create_ValidPost_PersistsPackageForActiveGym()
    {
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/MembershipPackages/Create");

        const string packageName = "MVC-CRT Phase 6 Monthly";

        var response = await client.PostAsync("/Admin/MembershipPackages/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = packageName,
            ["PackageType"] = nameof(MembershipPackageType.Monthly),
            ["DurationValue"] = "1",
            ["DurationUnit"] = nameof(DurationUnit.Month),
            ["BasePrice"] = "79",
            ["CurrencyCode"] = "EUR",
            ["TrainingDiscountPercent"] = "",
            ["IsTrainingFree"] = "false",
            ["Description"] = "MVC create flow."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/MembershipPackages", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var created = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .Where(package => package.GymId == gymId)
            .ToListAsync();

        Assert.Contains(created, package => PackageNameMatches(package.Name, packageName));
        var newPackage = created.Single(package => PackageNameMatches(package.Name, packageName));
        Assert.Equal(79m, newPackage.BasePrice);
        Assert.Equal("EUR", newPackage.CurrencyCode);
        Assert.Equal(1, newPackage.DurationValue);
    }

    [Fact]
    public async Task AdminMembershipPackages_Edit_AuthorizedAdminCanOpenForm()
    {
        var packageId = await GetSeededPackageIdAsync("Day Pass");
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync($"/Admin/MembershipPackages/Edit/{packageId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Day Pass", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminMembershipPackages_Edit_ValidPost_UpdatesPackage()
    {
        // Use a package not consumed by seeded memberships (Annual Performance is unsold in seed).
        var packageId = await GetSeededPackageIdAsync("Annual Performance");
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/MembershipPackages/Edit/{packageId}");

        const string updatedName = "Annual Performance (MVC Edited)";

        var response = await client.PostAsync($"/Admin/MembershipPackages/Edit/{packageId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = packageId.ToString(),
            ["Name"] = updatedName,
            ["PackageType"] = nameof(MembershipPackageType.Yearly),
            ["DurationValue"] = "1",
            ["DurationUnit"] = nameof(DurationUnit.Year),
            ["BasePrice"] = "749",
            ["CurrencyCode"] = "EUR",
            ["TrainingDiscountPercent"] = "60",
            ["IsTrainingFree"] = "false",
            ["Description"] = "Updated annual perf terms."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/MembershipPackages", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .SingleAsync(package => package.Id == packageId);

        Assert.True(PackageNameMatches(updated.Name, updatedName),
            $"Expected package name to contain '{updatedName}', actual primary value: '{PackageNamePrimary(updated.Name)}'.");
        Assert.Equal(749m, updated.BasePrice);
        Assert.Equal(60, updated.TrainingDiscountPercent);
    }

    [Fact]
    public async Task AdminMembershipPackages_Delete_RemovesSoftDeletesOrDeactivatesPackage()
    {
        // Insert an unused package so the deletion is not blocked by an active membership snapshot.
        Guid packageId;
        const string scratchName = "MVC-DEL Scratch Package";
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.SingleAsync(entity => entity.Code == GymCode);

            var package = new MembershipPackage
            {
                GymId = gym.Id,
                Name = new LangStr(scratchName, "en"),
                Description = new LangStr("Scratch for MVC delete test.", "en"),
                PackageType = MembershipPackageType.Monthly,
                DurationValue = 1,
                DurationUnit = DurationUnit.Month,
                BasePrice = 49m,
                CurrencyCode = "EUR",
                IsTrainingFree = false,
                TrainingDiscountPercent = 0
            };

            dbContext.MembershipPackages.Add(package);
            await dbContext.SaveChangesAsync();
            packageId = package.Id;
        }

        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/MembershipPackages/Delete/{packageId}");

        var response = await client.PostAsync($"/Admin/MembershipPackages/Delete/{packageId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = packageId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/MembershipPackages", location, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await verifyContext.MembershipPackages
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(package => package.Id == packageId);

        // Domain behavior may be hard delete, soft delete (IsDeleted), or deactivate (ValidTo set in the past).
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var removedOrInactive =
            afterDelete is null
            || afterDelete.IsDeleted
            || (afterDelete.ValidTo.HasValue && afterDelete.ValidTo.Value <= today);

        Assert.True(
            removedOrInactive,
            "After delete the package must be removed, soft-deleted, or deactivated (ValidTo in past).");

        // Whatever the storage outcome, the package must no longer surface in the active listing.
        var listClient = await CreateMvcClientAsync(AdminEmail);
        var listResponse = await listClient.GetAsync("/Admin/MembershipPackages");
        listResponse.EnsureSuccessStatusCode();
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(scratchName, listHtml);
    }

    [Fact]
    public async Task AdminMembershipPackages_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/MembershipPackages");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            var location = response.Headers.Location?.OriginalString ?? string.Empty;
            Assert.DoesNotMatch(@"/Admin/MembershipPackages(?:[/?]|$)", location);
        }
    }

    [Fact]
    public async Task AdminMembershipPackages_CrossTenantPackageId_Returns403Or404()
    {
        var peakForgePackageId = await GetSeededPackageIdAsync("Monthly Unlimited");
        var client = await CreateMvcClientAsync(MultiGymAdminEmail, allowAutoRedirect: false);

        var response = await client.GetAsync($"/Admin/MembershipPackages/Edit/{peakForgePackageId}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.Redirect,
            $"Expected 403/404/redirect for cross-tenant access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Fail("Cross-tenant package edit must not return 200 OK.");
        }
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email, bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
        var token = await GetFormAntiforgeryTokenAsync(client, "/");

        var loginResponse = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = Password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.True(
            loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == HttpStatusCode.Redirect,
            $"Login failed for {email} with status {(int)loginResponse.StatusCode}.");

        return client;
    }

    private static async Task<string> GetFormAntiforgeryTokenAsync(HttpClient client, string formPath)
    {
        var response = await client.GetAsync(formPath);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Could not load form page '{formPath}' to extract antiforgery token (status {(int)response.StatusCode}).");

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, $"Could not extract antiforgery token from '{formPath}'.");
        return match.Groups[1].Value;
    }

    private async Task<Guid> GetSeededPackageIdAsync(string englishName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var packages = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .Where(package => package.GymId == gymId)
            .ToListAsync();

        var match = packages.SingleOrDefault(package => PackageNameMatches(package.Name, englishName));
        Assert.NotNull(match);
        return match!.Id;
    }

    private static bool PackageNameMatches(LangStr? name, string expected)
    {
        if (name is null)
        {
            return false;
        }

        return name.Values.Any(value =>
            value is not null
            && value.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    private static string PackageNamePrimary(LangStr? name)
    {
        if (name is null)
        {
            return "(null)";
        }

        return name.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "(empty)";
    }
}
