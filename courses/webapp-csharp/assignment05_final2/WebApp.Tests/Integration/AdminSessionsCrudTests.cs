using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using Base.Domain;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Contract tests for the Admin/Sessions MVC CRUD workflow. The admin area used to
// expose /Admin/Sessions as a read-only list that deep-linked into the SaaS client;
// these tests pin the dedicated in-area CRUD (GET form + POST handler), tenant
// isolation, and authorization gating.
public class AdminSessionsCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminSessions_Index_AuthorizedAdminSeesAddLink()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Sessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("/Admin/Sessions/Create", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminSessions_Create_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Sessions/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", html);
        Assert.Contains("StartAt", html);
        Assert.Contains("EndAt", html);
        Assert.Contains("CategoryId", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminSessions_Create_InvalidPost_ReturnsValidationErrors()
    {
        var categoryId = await GetSeededCategoryIdAsync();
        var client = await CreateMvcClientAsync(AdminEmail);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Sessions/Create");

        // End before start + blank name should fail validation.
        var response = await client.PostAsync("/Admin/Sessions/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CategoryId"] = categoryId.ToString(),
            ["Name"] = "",
            ["StartAt"] = "2026-06-15T12:00",
            ["EndAt"] = "2026-06-15T11:00",
            ["Capacity"] = "10",
            ["BasePrice"] = "20",
            ["CurrencyCode"] = "EUR",
            ["Status"] = nameof(TrainingSessionStatus.Draft)
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches(
            @"(field-validation-error|input-validation-error|validation-summary-errors)",
            html);
    }

    [Fact]
    public async Task AdminSessions_Create_ValidPost_PersistsSessionForActiveGym()
    {
        var categoryId = await GetSeededCategoryIdAsync();
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Sessions/Create");

        const string sessionName = "MVC-CRT Phase 3b Session";

        var response = await client.PostAsync("/Admin/Sessions/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CategoryId"] = categoryId.ToString(),
            ["Name"] = sessionName,
            ["Description"] = "Created from the admin MVC flow.",
            ["StartAt"] = "2026-06-15T10:00",
            ["EndAt"] = "2026-06-15T11:00",
            ["Capacity"] = "12",
            ["BasePrice"] = "25",
            ["CurrencyCode"] = "EUR",
            ["Status"] = nameof(TrainingSessionStatus.Published)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Sessions", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var sessions = await dbContext.TrainingSessions
            .IgnoreQueryFilters()
            .Where(session => session.GymId == gymId)
            .ToListAsync();

        var created = sessions.SingleOrDefault(session => LangStrMatches(session.Name, sessionName));
        Assert.NotNull(created);
        Assert.Equal(12, created!.Capacity);
        Assert.Equal(25m, created.BasePrice);
        Assert.Equal(TrainingSessionStatus.Published, created.Status);
    }

    [Fact]
    public async Task AdminSessions_Edit_ValidPost_UpdatesSession()
    {
        var categoryId = await GetSeededCategoryIdAsync();
        var sessionId = await CreateScratchSessionAsync("MVC-EDIT Scratch Session", categoryId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Sessions/Edit/{sessionId}");

        const string updatedName = "MVC-EDIT Scratch Session (Edited)";

        var response = await client.PostAsync($"/Admin/Sessions/Edit/{sessionId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = sessionId.ToString(),
            ["CategoryId"] = categoryId.ToString(),
            ["Name"] = updatedName,
            ["StartAt"] = "2026-07-01T09:00",
            ["EndAt"] = "2026-07-01T10:30",
            ["Capacity"] = "20",
            ["BasePrice"] = "30",
            ["CurrencyCode"] = "EUR",
            ["Status"] = nameof(TrainingSessionStatus.Published)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.TrainingSessions
            .IgnoreQueryFilters()
            .SingleAsync(session => session.Id == sessionId);

        Assert.True(LangStrMatches(updated.Name, updatedName));
        Assert.Equal(20, updated.Capacity);
        Assert.Equal(30m, updated.BasePrice);
    }

    [Fact]
    public async Task AdminSessions_Delete_RemovesSession()
    {
        var categoryId = await GetSeededCategoryIdAsync();
        var sessionId = await CreateScratchSessionAsync("MVC-DEL Scratch Session", categoryId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Sessions/Delete/{sessionId}");

        var response = await client.PostAsync($"/Admin/Sessions/Delete/{sessionId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = sessionId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await dbContext.TrainingSessions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(session => session.Id == sessionId);

        var removedOrInactive = afterDelete is null || afterDelete.IsDeleted;
        Assert.True(removedOrInactive, "After delete the session must be removed or soft-deleted.");
    }

    [Fact]
    public async Task AdminSessions_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/Sessions/Create");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");
    }

    private async Task<Guid> CreateScratchSessionAsync(string name, Guid categoryId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var session = new App.Domain.Entities.TrainingSession
        {
            GymId = gymId,
            CategoryId = categoryId,
            Name = new LangStr(name, "en"),
            StartAtUtc = new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc),
            EndAtUtc = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc),
            Capacity = 10,
            BasePrice = 15m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Draft
        };

        dbContext.TrainingSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session.Id;
    }

    private async Task<Guid> GetSeededCategoryIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var categoryId = await dbContext.TrainingCategories
            .IgnoreQueryFilters()
            .Where(category => category.GymId == gymId)
            .Select(category => category.Id)
            .FirstAsync();

        return categoryId;
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

    private static bool LangStrMatches(LangStr? name, string expected)
    {
        if (name is null)
        {
            return false;
        }

        return name.Values.Any(value =>
            value is not null
            && value.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }
}
