using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Contract tests for the Admin/Operations equipment MVC CRUD workflow. The admin
// area used to expose /Admin/Operations as a read-only list; these tests pin the
// in-area equipment create/edit/delete flow, with tenant + role gating.
public class AdminOperationsEquipmentCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminOperations_Index_AuthorizedAdminSeesAddEquipmentLink()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Operations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("/Admin/Operations/EquipmentCreate", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminOperations_EquipmentCreate_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Operations/EquipmentCreate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("EquipmentModelId", html);
        Assert.Contains("AssetTag", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminOperations_EquipmentCreate_ValidPost_PersistsEquipmentForActiveGym()
    {
        var modelId = await GetSeededEquipmentModelIdAsync();
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Operations/EquipmentCreate");

        var assetTag = $"MVC-EQ-{Guid.NewGuid():N}"[..12];

        var response = await client.PostAsync("/Admin/Operations/EquipmentCreate", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["EquipmentModelId"] = modelId.ToString(),
            ["AssetTag"] = assetTag,
            ["SerialNumber"] = "SN-MVC-1",
            ["CurrentStatus"] = nameof(EquipmentStatus.Active),
            ["Notes"] = "Created from admin MVC flow."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Operations", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = await dbContext.Equipment
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.AssetTag == assetTag);

        Assert.NotNull(created);
        Assert.Equal(modelId, created!.EquipmentModelId);
    }

    [Fact]
    public async Task AdminOperations_EquipmentEdit_ValidPost_UpdatesEquipment()
    {
        var modelId = await GetSeededEquipmentModelIdAsync();
        var equipmentId = await CreateScratchEquipmentAsync(modelId, "MVC-EQ-EDIT");
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Operations/EquipmentEdit/{equipmentId}");

        var response = await client.PostAsync($"/Admin/Operations/EquipmentEdit/{equipmentId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = equipmentId.ToString(),
            ["EquipmentModelId"] = modelId.ToString(),
            ["AssetTag"] = "MVC-EQ-EDIT2",
            ["SerialNumber"] = "SN-EDIT",
            ["CurrentStatus"] = nameof(EquipmentStatus.Maintenance),
            ["Notes"] = "Updated."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.Equipment
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == equipmentId);

        Assert.Equal("MVC-EQ-EDIT2", updated.AssetTag);
        Assert.Equal(EquipmentStatus.Maintenance, updated.CurrentStatus);
    }

    [Fact]
    public async Task AdminOperations_EquipmentDelete_RemovesEquipment()
    {
        var modelId = await GetSeededEquipmentModelIdAsync();
        var equipmentId = await CreateScratchEquipmentAsync(modelId, "MVC-EQ-DEL");
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Operations/EquipmentDelete/{equipmentId}");

        var response = await client.PostAsync($"/Admin/Operations/EquipmentDelete/{equipmentId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = equipmentId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await dbContext.Equipment
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == equipmentId);

        var removedOrInactive = afterDelete is null || afterDelete.IsDeleted;
        Assert.True(removedOrInactive, "After delete the equipment must be removed or soft-deleted.");
    }

    [Fact]
    public async Task AdminOperations_EquipmentCreate_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/Operations/EquipmentCreate");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");
    }

    private async Task<Guid> CreateScratchEquipmentAsync(Guid modelId, string assetTagPrefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var equipment = new Equipment
        {
            GymId = gymId,
            EquipmentModelId = modelId,
            AssetTag = $"{assetTagPrefix}-{Guid.NewGuid():N}"[..16],
            CurrentStatus = EquipmentStatus.Active
        };

        dbContext.Equipment.Add(equipment);
        await dbContext.SaveChangesAsync();
        return equipment.Id;
    }

    private async Task<Guid> GetSeededEquipmentModelIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        return await dbContext.EquipmentModels
            .IgnoreQueryFilters()
            .Where(model => model.GymId == gymId)
            .Select(model => model.Id)
            .FirstAsync();
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
}
