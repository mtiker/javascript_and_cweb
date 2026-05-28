using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Contract tests for the Admin/Operations maintenance-task MVC workflow: in-area
// create, status/assignment edit, and delete, with tenant + role gating.
public class AdminOperationsTaskCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminOperations_TaskCreate_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Operations/TaskCreate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("EquipmentId", html);
        Assert.Contains("TaskType", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminOperations_TaskCreate_ValidPost_PersistsTaskForActiveGym()
    {
        var equipmentId = await GetSeededEquipmentIdAsync();
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Operations/TaskCreate");

        var response = await client.PostAsync("/Admin/Operations/TaskCreate", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["EquipmentId"] = equipmentId.ToString(),
            ["TaskType"] = nameof(MaintenanceTaskType.Scheduled),
            ["Priority"] = nameof(MaintenancePriority.High),
            ["Status"] = nameof(MaintenanceTaskStatus.Open),
            ["DueAt"] = "2026-07-10T09:00",
            ["Notes"] = "MVC created task."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Operations", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = await dbContext.MaintenanceTasks
            .IgnoreQueryFilters()
            .Where(task => task.EquipmentId == equipmentId && task.Priority == MaintenancePriority.High && task.Notes == "MVC created task.")
            .FirstOrDefaultAsync();

        Assert.NotNull(created);
    }

    [Fact]
    public async Task AdminOperations_TaskEdit_ValidPost_UpdatesStatusAndAssignment()
    {
        var equipmentId = await GetSeededEquipmentIdAsync();
        var staffId = await GetSeededStaffIdAsync();
        var taskId = await CreateScratchTaskAsync(equipmentId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Operations/TaskEdit/{taskId}");

        var response = await client.PostAsync($"/Admin/Operations/TaskEdit/{taskId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = taskId.ToString(),
            ["Status"] = nameof(MaintenanceTaskStatus.InProgress),
            ["AssignedStaffId"] = staffId.ToString(),
            ["Notes"] = "Picked up."
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.MaintenanceTasks
            .IgnoreQueryFilters()
            .SingleAsync(task => task.Id == taskId);

        Assert.Equal(MaintenanceTaskStatus.InProgress, updated.Status);
        Assert.Equal(staffId, updated.AssignedStaffId);
    }

    [Fact]
    public async Task AdminOperations_TaskDelete_RemovesTask()
    {
        var equipmentId = await GetSeededEquipmentIdAsync();
        var taskId = await CreateScratchTaskAsync(equipmentId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Operations/TaskDelete/{taskId}");

        var response = await client.PostAsync($"/Admin/Operations/TaskDelete/{taskId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = taskId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await dbContext.MaintenanceTasks
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(task => task.Id == taskId);

        var removedOrInactive = afterDelete is null || afterDelete.IsDeleted;
        Assert.True(removedOrInactive, "After delete the maintenance task must be removed or soft-deleted.");
    }

    [Fact]
    public async Task AdminOperations_TaskCreate_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/Operations/TaskCreate");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");
    }

    private async Task<Guid> CreateScratchTaskAsync(Guid equipmentId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var task = new MaintenanceTask
        {
            GymId = gymId,
            EquipmentId = equipmentId,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceTaskStatus.Open
        };

        dbContext.MaintenanceTasks.Add(task);
        await dbContext.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> GetSeededEquipmentIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        return await dbContext.Equipment
            .IgnoreQueryFilters()
            .Where(item => item.GymId == gymId)
            .Select(item => item.Id)
            .FirstAsync();
    }

    private async Task<Guid> GetSeededStaffIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        return await dbContext.Staff
            .IgnoreQueryFilters()
            .Where(member => member.GymId == gymId)
            .Select(member => member.Id)
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
