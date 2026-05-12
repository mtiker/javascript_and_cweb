using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

public class ClientDashboardTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ClientDashboard_RendersSeededMvcDashboard()
    {
        var client = await CreateMvcClientAsync("member@peakforge.local");

        var response = await client.GetAsync("/mvc-client");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("/css/site.css", html);
        Assert.Contains("class=\"topbar\"", html);
        Assert.Contains("peak-forge", html);
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "GymStrong123!",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));

        response.EnsureSuccessStatusCode();
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
}
