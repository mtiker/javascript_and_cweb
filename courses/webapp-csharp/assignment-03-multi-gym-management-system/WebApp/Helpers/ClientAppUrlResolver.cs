namespace WebApp.Helpers;

public static class ClientAppUrlResolver
{
    private const string DefaultBaseUrl = "/client";

    public static string GetBaseUrl(IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["ClientApp:BaseUrl"];
        return string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? DefaultBaseUrl
            : configuredBaseUrl.TrimEnd('/');
    }

    public static string GetRouteUrl(IConfiguration configuration, string routePath)
    {
        var normalizedRoutePath = routePath.StartsWith('/')
            ? routePath
            : $"/{routePath}";

        return $"{GetBaseUrl(configuration)}{normalizedRoutePath}";
    }
}
