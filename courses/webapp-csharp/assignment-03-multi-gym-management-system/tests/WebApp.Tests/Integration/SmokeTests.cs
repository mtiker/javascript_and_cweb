using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.DTO.v1.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class SmokeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task HomePage_ReturnsSuccess()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_UsesSharedLayoutAndSiteStyles()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        await AssertStyledPageAsync(client, "/Admin", "peak-forge");
    }

    [Fact]
    public async Task SeededMvcPages_RenderWithSharedLayoutAndStyles()
    {
        var sessionId = await GetPeakForgeSessionIdAsync();
        var maintenanceTaskId = await GetPeakForgeMaintenanceTaskIdAsync();

        var systemAdminClient = await CreateMvcClientAsync("systemadmin@gym.local");
        await AssertStyledPageAsync(systemAdminClient, "/Admin/Gyms", "Peak Forge Gym");

        var adminClient = await CreateMvcClientAsync("admin@peakforge.local");
        await AssertStyledPageAsync(adminClient, "/Admin/Memberships", "Memberships");
        await AssertStyledPageAsync(adminClient, "/Admin/Sessions", "Sessions");
        await AssertStyledPageAsync(adminClient, "/Admin/Operations", "Operations");

        var memberClient = await CreateMvcClientAsync("member@peakforge.local");
        await AssertStyledPageAsync(memberClient, "/mvc-client", "peak-forge");
        await AssertStyledPageAsync(memberClient, "/mvc-client/Profile", "MEM-001");
        await AssertStyledPageAsync(memberClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(memberClient, $"/mvc-client/Sessions/Details/{sessionId}", "Capacity");

        var trainerClient = await CreateMvcClientAsync("trainer@peakforge.local");
        await AssertStyledPageAsync(trainerClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(trainerClient, $"/mvc-client/Sessions/Roster/{sessionId}", "Trainer roster");

        var caretakerClient = await CreateMvcClientAsync("caretaker@peakforge.local");
        await AssertStyledPageAsync(caretakerClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(caretakerClient, "/mvc-client/Maintenance", "Maintenance");
        await AssertStyledPageAsync(caretakerClient, $"/mvc-client/Maintenance/Details/{maintenanceTaskId}", "Maintenance task");
    }

    [Fact]
    public async Task MemberCannotOpenTrainerRoster()
    {
        var sessionId = await GetPeakForgeSessionIdAsync();
        var client = await CreateMvcClientAsync("member@peakforge.local", allowAutoRedirect: false);

        var response = await client.GetAsync($"/mvc-client/Sessions/Roster/{sessionId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/access-denied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Register_ReturnsJwtPayload()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/account/register", new RegisterRequest
        {
            Email = "new.user@test.local",
            Password = "Gym123!",
            FirstName = "New",
            LastName = "User"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
    }

    [Fact]
    public async Task Login_SeededMultiGymAdmin_CanSwitchGym()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "multigym.admin@gym.local",
            Password = "Gym123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.ActiveGymCode));

        var targetGym = loginPayload.ActiveGymCode == "peak-forge" ? "north-star" : "peak-forge";
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var switchResponse = await client.PostAsJsonAsync("/api/v1/account/switch-gym", new SwitchGymRequest
        {
            GymCode = targetGym
        });

        switchResponse.EnsureSuccessStatusCode();
        var switchedPayload = await switchResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(switchedPayload);
        Assert.Equal(targetGym, switchedPayload!.ActiveGymCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "Could not extract antiforgery token from the rendered login page.");

        return match.Groups[1].Value;
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email, bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "Gym123!",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));

        if (allowAutoRedirect)
        {
            response.EnsureSuccessStatusCode();
        }
        else
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        return client;
    }

    private static async Task AssertStyledPageAsync(HttpClient client, string path, string expectedContent)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("/css/site.css", html);
        Assert.Contains("class=\"topbar\"", html);
        Assert.Contains(expectedContent, html);
    }

    private async Task<Guid> GetPeakForgeSessionIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await GetPeakForgeGymIdAsync(dbContext);

        return await dbContext.TrainingSessions
            .Where(entity => entity.GymId == gymId)
            .Select(entity => entity.Id)
            .FirstAsync();
    }

    private async Task<Guid> GetPeakForgeMaintenanceTaskIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await GetPeakForgeGymIdAsync(dbContext);

        return await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId)
            .Select(entity => entity.Id)
            .FirstAsync();
    }

    private static async Task<Guid> GetPeakForgeGymIdAsync(AppDbContext dbContext)
    {
        return await dbContext.Gyms
            .Where(entity => entity.Code == "peak-forge")
            .Select(entity => entity.Id)
            .FirstAsync();
    }
}
