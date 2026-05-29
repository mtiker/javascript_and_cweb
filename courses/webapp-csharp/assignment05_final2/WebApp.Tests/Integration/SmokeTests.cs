using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using App.DAL.EF;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Dtos.v1.Identity;
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
    public async Task SwaggerJson_ExposesV1DocumentAndJwtBearerSecurity()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Swagger JSON returned {(int)response.StatusCode}: {content}");
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("openapi", out _), "Swagger document must expose the OpenAPI version.");
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/v1/account/login", out _));
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/v1/{gymCode}/members", out _));

        var bearer = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task AdminDashboard_UsesSharedLayoutAndSiteStyles()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        await AssertStyledPageAsync(client, "/Admin", "peak-forge");
    }

    [Fact]
    public async Task AdminDashboard_QuickLinks_StayInAdminAreaWithSingleClientEntry()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        var response = await client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        // Admin work happens inside the admin area, so the quick links point there.
        Assert.Contains("/Admin/Members", html);
        Assert.Contains("/Admin/Memberships", html);
        Assert.Contains("/Admin/MembershipPackages", html);
        Assert.Contains("/Admin/Sessions", html);
        Assert.Contains("/Admin/TrainingCategories", html);
        Assert.Contains("/Admin/Operations", html);

        // A single entry point into the SaaS client, with no per-feature deep links.
        Assert.Contains("href=\"/client\"", html);
        Assert.DoesNotContain("href=\"/client/members\"", html);
        Assert.DoesNotContain("href=\"/client/sessions\"", html);
        Assert.DoesNotContain("href=\"/client/training-categories\"", html);
        Assert.DoesNotContain("href=\"/client/membership-packages\"", html);
        Assert.DoesNotContain("href=\"/client/finance-workspace\"", html);
        Assert.DoesNotContain("href=\"/client/maintenance\"", html);
    }

    [Fact]
    public async Task SeededMvcPages_RenderWithSharedLayoutAndStyles()
    {
        var sessionId = await GetPeakForgeSessionIdAsync();
        var maintenanceTaskId = await GetPeakForgeMaintenanceTaskIdAsync();

        var memberClient = await CreateMvcClientAsync("member@peakforge.local");
        await AssertStyledPageAsync(memberClient, "/mvc-client", "peak-forge");
        await AssertStyledPageAsync(memberClient, "/mvc-client/Profile", "MEM-001");
        await AssertStyledPageAsync(memberClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(memberClient, $"/mvc-client/Sessions/Details/{sessionId}", "Mahutavus");

        var trainerClient = await CreateMvcClientAsync("trainer@peakforge.local");
        await AssertStyledPageAsync(trainerClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(trainerClient, $"/mvc-client/Sessions/Roster/{sessionId}", "Treeneri nimekiri");

        var caretakerClient = await CreateMvcClientAsync("caretaker@peakforge.local");
        await AssertStyledPageAsync(caretakerClient, "/mvc-client/Sessions", "peak-forge");
        await AssertStyledPageAsync(caretakerClient, "/mvc-client/Maintenance", "Maintenance");
        await AssertStyledPageAsync(caretakerClient, $"/mvc-client/Maintenance/Details/{maintenanceTaskId}", "Hooldustöö");
    }

    [Fact]
    public async Task SystemAdmin_GymsRoute_RendersMvcPage()
    {
        var client = await CreateMvcClientAsync("systemadmin@gym.local");

        await AssertStyledPageAsync(client, "/Admin/Gyms", "peak-forge");
    }

    [Fact]
    public async Task TenantAdmin_WorkspaceRoutes_RenderMvcPages()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        await AssertStyledPageAsync(client, "/Admin/Memberships", "peak-forge");
        await AssertStyledPageAsync(client, "/Admin/Sessions", "peak-forge");
        await AssertStyledPageAsync(client, "/Admin/Operations", "peak-forge");
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
            Password = "GymStrong123!",
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
            Password = "GymStrong123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.ActiveGymCode));
        Assert.Contains(loginPayload.AvailableTenants, tenant => tenant.GymCode == "peak-forge");
        Assert.Contains(loginPayload.AvailableTenants, tenant => tenant.GymCode == "north-star");

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
        var switchedTenant = Assert.Single(switchedPayload.AvailableTenants, tenant => tenant.GymCode == targetGym);
        Assert.Contains(switchedPayload.ActiveRole!, switchedTenant.Roles);
    }

    [Fact]
    public async Task AccountPublicApi_LoginRefreshAndLogout_StillWorkThroughStableRoutes()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "admin@peakforge.local",
            Password = "GymStrong123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JwtResponse>();
        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.RefreshToken));

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/account/renew-refresh-token", new RefreshTokenRequest
        {
            Jwt = loginPayload.Jwt,
            RefreshToken = loginPayload.RefreshToken
        });

        refreshResponse.EnsureSuccessStatusCode();
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<JwtResponse>();
        Assert.NotNull(refreshPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload!.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshPayload.Jwt);
        var logoutResponse = await client.PostAsync("/api/v1/account/logout", content: null);

        logoutResponse.EnsureSuccessStatusCode();
        var message = await logoutResponse.Content.ReadFromJsonAsync<Shared.Contracts.Dtos.v1.Message>();
        Assert.NotNull(message);
        Assert.Contains("Logged out.", message!.Messages);
    }

    [Fact]
    public async Task AccountPublicApi_SystemAdmin_CanSwitchRoleInsideSelectedGym()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "systemadmin@gym.local",
            Password = "GymStrong123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JwtResponse>();
        Assert.NotNull(loginPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Jwt);

        var gymResponse = await client.PostAsJsonAsync("/api/v1/account/switch-gym", new SwitchGymRequest
        {
            GymCode = "north-star"
        });

        gymResponse.EnsureSuccessStatusCode();
        var gymPayload = await gymResponse.Content.ReadFromJsonAsync<JwtResponse>();
        Assert.NotNull(gymPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gymPayload!.Jwt);

        var roleResponse = await client.PostAsJsonAsync("/api/v1/account/switch-role", new SwitchRoleRequest
        {
            RoleName = "GymAdmin"
        });

        roleResponse.EnsureSuccessStatusCode();
        var rolePayload = await roleResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(rolePayload);
        Assert.Equal("north-star", rolePayload!.ActiveGymCode);
        Assert.Equal("GymAdmin", rolePayload.ActiveRole);
        Assert.Contains("SystemAdmin", rolePayload.SystemRoles);
    }

    [Fact]
    public async Task SystemAdmin_CanSelectAnyActiveGymContext()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "systemadmin@gym.local",
            Password = "GymStrong123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JwtResponse>();
        Assert.NotNull(loginPayload);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginPayload!.Jwt);

        var switchResponse = await client.PostAsJsonAsync("/api/v1/account/switch-gym", new SwitchGymRequest
        {
            GymCode = "north-star"
        });

        switchResponse.EnsureSuccessStatusCode();
        var switchedPayload = await switchResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(switchedPayload);
        Assert.Equal("north-star", switchedPayload!.ActiveGymCode);
        Assert.Equal("GymOwner", switchedPayload.ActiveRole);
        Assert.Contains("SystemAdmin", switchedPayload.SystemRoles);
    }

    [Fact]
    public async Task SplitTenantApiControllers_KeepExistingReadRoutes()
    {
        var client = await CreateAuthenticatedApiClientAsync("admin@peakforge.local");
        var paths = new[]
        {
            "/api/v1/peak-forge/staff",
            "/api/v1/peak-forge/training-categories",
            "/api/v1/peak-forge/training-sessions",
            "/api/v1/peak-forge/bookings",
            "/api/v1/peak-forge/membership-packages",
            "/api/v1/peak-forge/memberships",
            "/api/v1/peak-forge/payments",
            "/api/v1/peak-forge/equipment-models",
            "/api/v1/peak-forge/equipment",
            "/api/v1/peak-forge/maintenance-tasks",
            "/api/v1/peak-forge/gym-settings",
            "/api/v1/peak-forge/gym-users"
        };

        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();
        }
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
            ["Password"] = "GymStrong123!",
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

    private async Task<HttpClient> CreateAuthenticatedApiClientAsync(string email)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = "GymStrong123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);
        return client;
    }

    private static async Task AssertStyledPageAsync(HttpClient client, string path, string expectedContent)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("/css/site.css", html);
        Assert.Contains("class=\"area-toolbar", html);
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
