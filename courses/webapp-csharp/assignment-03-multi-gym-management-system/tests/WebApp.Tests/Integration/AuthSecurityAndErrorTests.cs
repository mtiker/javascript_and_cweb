using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Entities;
using App.DTO.v1.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class AuthSecurityAndErrorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task RenewRefreshToken_RotatesToken_AndRejectsReuse()
    {
        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "admin@peakforge.local", "Gym123!");

        var renewedResponse = await client.PostAsJsonAsync("/api/v1/account/renew-refresh-token", new RefreshTokenRequest
        {
            Jwt = loginPayload.Jwt,
            RefreshToken = loginPayload.RefreshToken
        });

        renewedResponse.EnsureSuccessStatusCode();
        var renewedPayload = await renewedResponse.Content.ReadFromJsonAsync<JwtResponse>();

        Assert.NotNull(renewedPayload);
        Assert.NotEqual(loginPayload.RefreshToken, renewedPayload!.RefreshToken);

        var reusedResponse = await client.PostAsJsonAsync("/api/v1/account/renew-refresh-token", new RefreshTokenRequest
        {
            Jwt = loginPayload.Jwt,
            RefreshToken = loginPayload.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Forbidden, reusedResponse.StatusCode);
    }

    [Fact]
    public async Task RenewRefreshToken_RejectsExpiredRefreshToken()
    {
        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "admin@peakforge.local", "Gym123!");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var refreshToken = await dbContext.RefreshTokens.FirstAsync(entity => entity.RefreshToken == loginPayload.RefreshToken);
            refreshToken.Expiration = DateTime.UtcNow.AddMinutes(-5);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/account/renew-refresh-token", new RefreshTokenRequest
        {
            Jwt = loginPayload.Jwt,
            RefreshToken = loginPayload.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MembersEndpoint_RejectsActiveGymMismatch()
    {
        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "admin@peakforge.local", "Gym123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var response = await client.GetAsync("/api/v1/north-star/members");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_CannotReadAnotherMember()
    {
        Guid otherMemberId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == "peak-forge");

            var person = new Person
            {
                FirstName = "Other",
                LastName = "Member",
                PersonalCode = "39901010001"
            };
            var otherMember = new Member
            {
                GymId = gym.Id,
                Person = person,
                MemberCode = "MEM-999"
            };

            dbContext.Members.Add(otherMember);
            await dbContext.SaveChangesAsync();
            otherMemberId = otherMember.Id;
        }

        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "member@peakforge.local", "Gym123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var response = await client.GetAsync($"/api/v1/peak-forge/members/{otherMemberId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SystemPlatformAnalytics_RejectsTenantOnlyUser()
    {
        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "member@peakforge.local", "Gym123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var response = await client.GetAsync("/api/v1/system/platform/analytics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApiErrors_ReturnProblemDetailsJson()
    {
        var client = factory.CreateClient();
        var loginPayload = await LoginAsync(client, "admin@peakforge.local", "Gym123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var response = await client.GetAsync($"/api/v1/peak-forge/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

[Fact]
    public async Task HtmlErrors_RenderHtmlErrorPage()
    {
        var productionFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = productionFactory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync("/set-culture", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en",
            ["returnUrl"] = "https://example.com/not-local",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("application/problem+json", response.Content.Headers.ContentType?.ToString() ?? string.Empty);
        Assert.Contains("Something went wrong", content);
    }

    private static async Task<JwtResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();
        return (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
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
