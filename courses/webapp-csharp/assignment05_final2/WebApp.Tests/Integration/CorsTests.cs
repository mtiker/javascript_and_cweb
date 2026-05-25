using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

public class CorsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CorsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApiPreflight_AllowsConfiguredClientOrigin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = CreatePreflightRequest("https://tests.multi-gym.local");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertHeader(response, "Access-Control-Allow-Origin", "https://tests.multi-gym.local");
        AssertHeaderContains(response, "Access-Control-Allow-Methods", "POST");
        AssertHeaderContains(response, "Access-Control-Allow-Headers", "authorization");
        AssertHeaderContains(response, "Access-Control-Allow-Headers", "content-type");
        AssertHeaderContains(response, "Access-Control-Allow-Headers", "accept-language");
    }

    [Fact]
    public async Task ApiPreflight_DoesNotAllowUnknownClientOrigin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = CreatePreflightRequest("https://unknown-client.example");
        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/account/login");
        request.Headers.TryAddWithoutValidation("Origin", origin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Headers",
            "authorization,content-type,accept-language");
        return request;
    }

    private static void AssertHeader(HttpResponseMessage response, string name, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(name, out var values), $"Missing {name} header.");
        Assert.Contains(expectedValue, values);
    }

    private static void AssertHeaderContains(HttpResponseMessage response, string name, string expectedToken)
    {
        Assert.True(response.Headers.TryGetValues(name, out var values), $"Missing {name} header.");
        Assert.Contains(values.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries)), value =>
            string.Equals(value, expectedToken, StringComparison.OrdinalIgnoreCase));
    }
}
