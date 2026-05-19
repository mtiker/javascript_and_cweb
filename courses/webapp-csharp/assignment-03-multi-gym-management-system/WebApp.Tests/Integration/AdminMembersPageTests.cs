using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

public class AdminMembersPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string ControllerSourcePath =
        "WebApp/Areas/Admin/Controllers/MembersController.cs";

    private const string ViewSourcePath =
        "WebApp/Areas/Admin/Views/Members/Index.cshtml";

    [Fact]
    public async Task AdminMembersPage_RendersRazorView()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        var response = await client.GetAsync("/Admin/Members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("/css/site.css", html);
        Assert.Contains("GymOps Admin", html);
        Assert.Contains("class=\"area-toolbar", html);
        Assert.Contains("bi bi-people", html);
    }

    [Fact]
    public async Task AdminMembersPage_RendersFromViewModel()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local");

        var response = await client.GetAsync("/Admin/Members");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // The seed data places at least four members in peak-forge (MEM-001..MEM-004).
        // The Razor view binds to a strongly-typed AdminMembersPageViewModel so it must
        // surface real data — the gym slug, the seeded member codes, and a count.
        Assert.Contains("peak-forge", html);
        Assert.Contains("MEM-001", html);
        Assert.Contains("MEM-002", html);
    }

    [Fact]
    public void AdminMembersPage_DoesNotUseViewBagOrViewData()
    {
        var repoRoot = ResolveAssignmentRoot();
        var controllerSource = File.ReadAllText(Path.Combine(repoRoot, ControllerSourcePath));
        var viewSource = File.ReadAllText(Path.Combine(repoRoot, ViewSourcePath));

        Assert.DoesNotContain("ViewBag", controllerSource);
        Assert.DoesNotContain("ViewData", controllerSource);
        Assert.DoesNotContain("ViewBag", viewSource);
        // ViewData["Title"] is sometimes set in _ViewStart; the page itself must not use it.
        Assert.DoesNotContain("ViewData[", viewSource);

        // The view must declare a strongly-typed model.
        Assert.Contains("@model", viewSource);
        Assert.Contains("AdminMembersPageViewModel", viewSource);
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var loginResponse = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "GymStrong123!",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));
        loginResponse.EnsureSuccessStatusCode();

        return client;
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

    private static string ResolveAssignmentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "multi-gym-management-system.slnx");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the assignment root (multi-gym-management-system.slnx) by walking up from " +
            AppContext.BaseDirectory);
    }
}
