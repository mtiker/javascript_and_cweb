using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

public class MvcComplianceTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly string[] TenantAdminRoutes =
    [
        "/Admin/Dashboard",
        "/Admin/Members",
        "/Admin/Memberships",
        "/Admin/Sessions",
        "/Admin/Operations"
    ];

    [Fact]
    public async Task AnonymousUser_CannotAccess_Admin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Fact]
    public async Task WrongRole_CannotAccess_Admin()
    {
        var client = await CreateMvcClientAsync("member@peakforge.local", allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("client", response.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("admin@peakforge.local")]
    [InlineData("systemadmin@gym.local")]
    public async Task GymAdminOrGymOwner_CanAccess_TenantAdminPages(string email)
    {
        var client = await CreateMvcClientAsync(email, allowAutoRedirect: false);

        foreach (var route in TenantAdminRoutes)
        {
            var response = await client.GetAsync(route);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
        }
    }

    [Theory]
    [InlineData("member@peakforge.local", "/mvc-client/Dashboard")]
    [InlineData("member@peakforge.local", "/mvc-client/Profile")]
    [InlineData("trainer@peakforge.local", "/mvc-client/Sessions")]
    [InlineData("caretaker@peakforge.local", "/mvc-client/Maintenance")]
    public async Task MvcClientRoute_Works_ForTenantRoles(string email, string route)
    {
        var client = await CreateMvcClientAsync(email);

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    [Fact]
    public void AdminViews_DoNotUse_ViewBagOrViewData()
    {
        var adminViewsRoot = Path.Combine(ResolveAssignmentRoot(), "WebApp", "Areas", "Admin", "Views");
        var razorFiles = Directory.GetFiles(adminViewsRoot, "*.cshtml", SearchOption.AllDirectories);

        Assert.NotEmpty(razorFiles);
        foreach (var file in razorFiles)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("ViewBag", source);
            Assert.DoesNotContain("ViewData", source);
        }
    }

    [Fact]
    public void AdminViews_RenderOnlyStronglyTypedViewModels()
    {
        var adminViewsRoot = Path.Combine(ResolveAssignmentRoot(), "WebApp", "Areas", "Admin", "Views");
        var razorFiles = Directory.GetFiles(adminViewsRoot, "*.cshtml", SearchOption.AllDirectories);

        Assert.NotEmpty(razorFiles);
        foreach (var file in razorFiles.Where(IsAdminPageView))
        {
            var source = File.ReadAllText(file);
            Assert.Matches(@"@model\s+Admin[A-Za-z]+ViewModel", source);
            Assert.DoesNotContain("@model dynamic", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Html.Raw", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AdminPostActions_UseAntiForgery()
    {
        var adminControllersRoot = Path.Combine(ResolveAssignmentRoot(), "WebApp", "Areas", "Admin", "Controllers");
        var controllerFiles = Directory.GetFiles(adminControllersRoot, "*Controller.cs", SearchOption.AllDirectories);
        var postActionPattern = new Regex(
            @"(?<attributes>(?:\s*\[[^\]]+\]\s*)+)\s*public\s+(?:async\s+)?(?:Task<)?IActionResult",
            RegexOptions.Compiled);

        Assert.NotEmpty(controllerFiles);
        foreach (var file in controllerFiles)
        {
            var source = File.ReadAllText(file);
            foreach (Match match in postActionPattern.Matches(source))
            {
                var attributes = match.Groups["attributes"].Value;
                if (!attributes.Contains("[HttpPost", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Contains("ValidateAntiForgeryToken", attributes);
            }
        }
    }

    [Fact]
    public async Task AdminPost_WithoutAntiForgeryToken_IsRejected()
    {
        var client = await CreateMvcClientAsync("admin@peakforge.local", allowAutoRedirect: false);

        var response = await client.PostAsync(
            "/Admin/Members/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["GivenName"] = "NoToken",
                ["FamilyName"] = "Should401",
                ["Status"] = "Active"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void AdminControllers_ReturnStronglyTypedViewModels()
    {
        var assignmentRoot = ResolveAssignmentRoot();
        var adminControllersRoot = Path.Combine(assignmentRoot, "WebApp", "Areas", "Admin", "Controllers");
        var controllerFiles = Directory.GetFiles(adminControllersRoot, "*Controller.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(controllerFiles);
        foreach (var file in controllerFiles)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("ClientAppUrlResolver.GetRouteUrl", source);
            Assert.DoesNotContain("Redirect(ClientAppUrlResolver", source);
            Assert.DoesNotContain("ViewBag", source);
            Assert.DoesNotContain("ViewData", source);

            Assert.DoesNotContain("return View();", source, StringComparison.Ordinal);
            Assert.Contains("return View(", source, StringComparison.Ordinal);
        }
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email, bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var loginResponse = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "GymStrong123!",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));
        Assert.True(
            loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == HttpStatusCode.Redirect,
            $"Login failed with status {(int)loginResponse.StatusCode}.");

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

    private static bool IsAdminPageView(string path)
    {
        var fileName = Path.GetFileName(path);
        return !fileName.StartsWith("_", StringComparison.Ordinal) &&
               !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Contains("Shared", StringComparer.OrdinalIgnoreCase);
    }
}
