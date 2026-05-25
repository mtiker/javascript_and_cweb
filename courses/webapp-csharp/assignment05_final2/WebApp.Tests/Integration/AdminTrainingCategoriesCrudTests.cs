using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using Base.Domain;
using App.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Failing-by-design contract tests for the Admin/TrainingCategories MVC CRUD workflow.
// The Admin area does not yet expose a TrainingCategories controller; these tests pin
// the expected shape of /Admin/TrainingCategories with full CRUD (GET form + POST handler),
// localized LangStr names, tenant isolation, and authorization gating.
public class AdminTrainingCategoriesCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string MultiGymAdminEmail = "multigym.admin@gym.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminTrainingCategories_Index_AuthorizedAdminCanOpen()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/TrainingCategories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Seeded peak-forge categories: "Strength Lab", "Conditioning", "Mobility and Recovery".
        Assert.Contains("Strength Lab", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminTrainingCategories_Create_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/TrainingCategories/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", html);
        Assert.Contains("Description", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminTrainingCategories_Create_InvalidPost_ReturnsValidationErrors()
    {
        var client = await CreateMvcClientAsync(AdminEmail);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/TrainingCategories/Create");

        var response = await client.PostAsync("/Admin/TrainingCategories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "",
            ["Description"] = ""
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches(
            @"(field-validation-error|input-validation-error|validation-summary-errors)",
            html);
    }

    [Fact]
    public async Task AdminTrainingCategories_Create_ValidPost_PersistsCategoryForActiveGym()
    {
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/TrainingCategories/Create");

        const string categoryName = "MVC-CRT Phase 6 Plyometrics";

        var response = await client.PostAsync("/Admin/TrainingCategories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = categoryName,
            ["Description"] = "MVC create flow."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/TrainingCategories", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var created = await dbContext.TrainingCategories
            .IgnoreQueryFilters()
            .Where(category => category.GymId == gymId)
            .ToListAsync();

        Assert.Contains(created, category => CategoryNameMatches(category.Name, categoryName));
    }

    [Fact]
    public async Task AdminTrainingCategories_Edit_AuthorizedAdminCanOpenForm()
    {
        var categoryId = await GetSeededCategoryIdAsync("Conditioning");
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync($"/Admin/TrainingCategories/Edit/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Conditioning", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminTrainingCategories_Edit_ValidPost_UpdatesCategory()
    {
        // Use the mobility category (no test has yet asserted on its name).
        var categoryId = await GetSeededCategoryIdAsync("Mobility and Recovery");
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/TrainingCategories/Edit/{categoryId}");

        const string updatedName = "Mobility and Recovery (MVC Edited)";

        var response = await client.PostAsync($"/Admin/TrainingCategories/Edit/{categoryId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = categoryId.ToString(),
            ["Name"] = updatedName,
            ["Description"] = "Updated mobility category description."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/TrainingCategories", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.TrainingCategories
            .IgnoreQueryFilters()
            .SingleAsync(category => category.Id == categoryId);

        Assert.True(CategoryNameMatches(updated.Name, updatedName),
            $"Expected category name to contain '{updatedName}', actual primary value: '{CategoryNamePrimary(updated.Name)}'.");
    }

    [Fact]
    public async Task AdminTrainingCategories_Delete_RemovesSoftDeletesOrDeactivatesCategory()
    {
        // Insert an unused category so the deletion is not blocked by an active training session.
        Guid categoryId;
        const string scratchName = "MVC-DEL Scratch Category";
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.SingleAsync(entity => entity.Code == GymCode);

            var category = new TrainingCategory
            {
                GymId = gym.Id,
                Name = new LangStr(scratchName, "en"),
                Description = new LangStr("Scratch for MVC delete test.", "en")
            };

            dbContext.TrainingCategories.Add(category);
            await dbContext.SaveChangesAsync();
            categoryId = category.Id;
        }

        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/TrainingCategories/Delete/{categoryId}");

        var response = await client.PostAsync($"/Admin/TrainingCategories/Delete/{categoryId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = categoryId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/TrainingCategories", location, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await verifyContext.TrainingCategories
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(category => category.Id == categoryId);

        // Domain behavior may be hard delete, soft delete (IsDeleted), or deactivate (ValidTo set in the past).
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var removedOrInactive =
            afterDelete is null
            || afterDelete.IsDeleted
            || (afterDelete.ValidTo.HasValue && afterDelete.ValidTo.Value <= today);

        Assert.True(
            removedOrInactive,
            "After delete the training category must be removed, soft-deleted, or deactivated (ValidTo in past).");

        // Whatever the storage outcome, the category must no longer surface in the active listing.
        var listClient = await CreateMvcClientAsync(AdminEmail);
        var listResponse = await listClient.GetAsync("/Admin/TrainingCategories");
        listResponse.EnsureSuccessStatusCode();
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(scratchName, listHtml);
    }

    [Fact]
    public async Task AdminTrainingCategories_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/TrainingCategories");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            var location = response.Headers.Location?.OriginalString ?? string.Empty;
            Assert.DoesNotMatch(@"/Admin/TrainingCategories(?:[/?]|$)", location);
        }
    }

    [Fact]
    public async Task AdminTrainingCategories_CrossTenantCategoryId_Returns403Or404()
    {
        var peakForgeCategoryId = await GetSeededCategoryIdAsync("Strength Lab");
        var client = await CreateMvcClientAsync(MultiGymAdminEmail, allowAutoRedirect: false);

        var response = await client.GetAsync($"/Admin/TrainingCategories/Edit/{peakForgeCategoryId}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.Redirect,
            $"Expected 403/404/redirect for cross-tenant access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Fail("Cross-tenant training category edit must not return 200 OK.");
        }
    }

    [Fact]
    public async Task AdminTrainingCategories_Index_RendersEstonianLangStrValueForEtCulture()
    {
        var client = await CreateMvcClientAsync(AdminEmail);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("et-EE");

        var response = await client.GetAsync("/Admin/TrainingCategories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Seeded "Strength Lab" has Estonian translation "Jõutreening".
        Assert.Contains("Jõutreening", html);
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

    private async Task<Guid> GetSeededCategoryIdAsync(string englishName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var categories = await dbContext.TrainingCategories
            .IgnoreQueryFilters()
            .Where(category => category.GymId == gymId)
            .ToListAsync();

        var match = categories.SingleOrDefault(category => CategoryNameMatches(category.Name, englishName));
        Assert.NotNull(match);
        return match!.Id;
    }

    private static bool CategoryNameMatches(LangStr? name, string expected)
    {
        if (name is null)
        {
            return false;
        }

        return name.Values.Any(value =>
            value is not null
            && value.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    private static string CategoryNamePrimary(LangStr? name)
    {
        if (name is null)
        {
            return "(null)";
        }

        return name.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "(empty)";
    }
}
