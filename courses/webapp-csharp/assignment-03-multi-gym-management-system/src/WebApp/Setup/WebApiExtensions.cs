using System.Globalization;
using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Setup;

public static class WebApiExtensions
{
    public static IServiceCollection AddAppLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("et-EE"),
                new CultureInfo("et"),
                new CultureInfo("en"),
                new CultureInfo("en-US")
            };

            options.DefaultRequestCulture = new RequestCulture("et-EE");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        return services;
    }

    public static IServiceCollection AddAppControllers(this IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();

        return services;
    }

    public static IServiceCollection AddAppForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardLimit = null;
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Any, 0));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.IPv6Any, 0));
        });

        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allowedCorsOrigins = environment.IsDevelopment()
            ? configuredOrigins is { Length: > 0 }
                ? configuredOrigins
                : ["http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173"]
            : ValidateProductionCorsOrigins(configuredOrigins);

        services.AddCors(options =>
        {
            options.AddPolicy("ClientApp", policy =>
            {
                policy.WithOrigins(allowedCorsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static string[] ValidateProductionCorsOrigins(string[]? configuredOrigins)
    {
        if (configuredOrigins is not { Length: > 0 })
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
        }

        var normalizedOrigins = new List<string>(configuredOrigins.Length);

        foreach (var configuredOrigin in configuredOrigins)
        {
            if (configuredOrigin.Contains('*', StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must not contain wildcard origins outside Development.");
            }

            if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var originUri) ||
                !string.Equals(originUri.GetLeftPart(UriPartial.Authority), configuredOrigin, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cors:AllowedOrigins entry '{configuredOrigin}' must be an absolute origin without path, query, or fragment.");
            }

            if (string.Equals(originUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                originUri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(originUri.Host, out var hostIp) && IPAddress.IsLoopback(hostIp))
            {
                throw new InvalidOperationException($"Cors:AllowedOrigins entry '{configuredOrigin}' is not allowed outside Development.");
            }

            normalizedOrigins.Add(originUri.GetLeftPart(UriPartial.Authority));
        }

        return normalizedOrigins
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IServiceCollection AddAppApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    public static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}
