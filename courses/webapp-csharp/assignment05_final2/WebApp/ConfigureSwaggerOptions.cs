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
                Title = $"Multi-Gym Management System API {description.ApiVersion}",
                Version = description.ApiVersion.ToString()
            });
        }

        options.CustomSchemaIds(type => type.FullName ?? type.Name);

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter a bearer token."
        });

        options.AddSecurityRequirement(_ =>
        {
            var requirement = new OpenApiSecurityRequirement();
            var schemeReference = new OpenApiSecuritySchemeReference("Bearer");
            requirement[schemeReference] = [];
            return requirement;
        });
    }
}
