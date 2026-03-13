using System.Net.Http.Json;
using App.DTO.v1.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

public class IntegrationTestIdentity : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IntegrationTestIdentity(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_Then_Login_ReturnsJwtAndRefreshToken()
    {
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var password = "Strong.Pass.123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/account/register", new Register
        {
            Email = email,
            Password = password
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<JWTResponse>();

        Assert.NotNull(registerPayload);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();

        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.RefreshToken));
    }
}
