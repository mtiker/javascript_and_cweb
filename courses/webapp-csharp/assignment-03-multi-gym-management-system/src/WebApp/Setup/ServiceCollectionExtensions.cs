using System.Globalization;
using System.Text;
using App.BLL.Contracts;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.DAL.EF.Tenant;
using App.Domain.Identity;
using App.Resources;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedCorsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                 ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToArray()
                             ?? ["http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173"];

        services.AddHttpContextAccessor();
        services.AddScoped<IGymContext, HttpGymContext>();

        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? "Host=localhost;Port=5432;Database=multi_gym_management_system;Username=postgres;Password=postgres";

            options.UseNpgsql(connectionString);

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";
            options.SlidingExpiration = true;
        });

        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "multi-gym-management-system",
                    ValidAudience = configuration["Jwt:Audience"] ?? "multi-gym-management-system",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "super-secret-assignment-03-key-change-me"))
                };
            });

        services.AddAuthorization();
        services.AddCors(options =>
        {
            options.AddPolicy("ClientApp", policy =>
            {
                policy.WithOrigins(allowedCorsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("et-EE"),
                new CultureInfo("en")
            };

            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("et-EE");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();

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

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddHealthChecks();

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IMemberWorkflowService, MemberWorkflowService>();
        services.AddScoped<IMembershipWorkflowService, MembershipWorkflowService>();
        services.AddScoped<ITrainingWorkflowService, TrainingWorkflowService>();
        services.AddScoped<IMaintenanceWorkflowService, MaintenanceWorkflowService>();

        return services;
    }
}
