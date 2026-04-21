using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Identity;

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
}
