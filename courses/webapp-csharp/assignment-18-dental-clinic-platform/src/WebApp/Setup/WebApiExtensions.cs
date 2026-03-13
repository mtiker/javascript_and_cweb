using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Setup;

public static class WebApiExtensions
{
    public static IServiceCollection AddAppControllers(this IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
            });

        return services;
    }

    public static IServiceCollection AddForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardLimit = null;
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Any, 0));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.IPv6Any, 0));
        });

        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy("CorsAllowAll", builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("X-Version", "X-Version-Created-At");

                if (environment.IsDevelopment())
                {
                    if (allowedOrigins is { Length: > 0 })
                    {
                        builder.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        builder.AllowAnyOrigin();
                    }
                }
                else
                {
                    if (allowedOrigins is not { Length: > 0 })
                    {
                        throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
                    }

                    builder.WithOrigins(allowedOrigins);
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddAppApiVersioning(this IServiceCollection services)
    {
        var builder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        });

        builder.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();

        return services;
    }
}
