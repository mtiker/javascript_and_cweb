using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"Dental Clinic SaaS API {description.ApiVersion}",
                Version = description.ApiVersion.ToString()
            });
        }

        options.CustomSchemaIds(type => type.FullName ?? type.Name);

        var bearerScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            Description = "Use format: Bearer {token}"
        };

        options.AddSecurityDefinition("Bearer", bearerScheme);

        options.AddSecurityRequirement(_ =>
        {
            var securityRequirement = new OpenApiSecurityRequirement();
            var schemeReference = new OpenApiSecuritySchemeReference("Bearer");
            securityRequirement[schemeReference] = new List<string>();
            return securityRequirement;
        });
    }
}
