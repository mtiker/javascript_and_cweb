using App.DAL.EF;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WebApp.Setup;

namespace WebApp.Tests.Unit;

public class RuntimeConfigurationTests
{
    [Fact]
    public void AddAppIdentity_RequiresJwtKey()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "MultiGymManagementSystem",
                ["Jwt:Audience"] = "MultiGymManagementSystem"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAppIdentity(configuration, TestWebHostEnvironment.Production()));

        Assert.Equal("Jwt:Key is missing.", exception.Message);
    }

    [Fact]
    public void AddAppIdentity_EnforcesStrongPasswordPolicy()
    {
        using var serviceProvider = BuildIdentityServiceProvider(Environments.Production);
        var options = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(8, options.Password.RequiredLength);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireNonAlphanumeric);
    }

    [Fact]
    public void AddAppIdentity_RequireHttpsMetadata_IsEnabledOutsideDevelopment()
    {
        using var productionProvider = BuildIdentityServiceProvider(Environments.Production);
        var productionOptions = productionProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        using var developmentProvider = BuildIdentityServiceProvider(Environments.Development);
        var developmentOptions = developmentProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(productionOptions.RequireHttpsMetadata);
        Assert.False(developmentOptions.RequireHttpsMetadata);
    }

    [Fact]
    public void AddAppCors_ProductionWithoutConfiguredOrigins_FailsFast()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAppCors(configuration, TestWebHostEnvironment.Production()));

        Assert.Equal("Cors:AllowedOrigins must be configured outside Development.", exception.Message);
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("https://*.example.com")]
    public void AddAppCors_ProductionRejectsUnsafeOrigins(string origin)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = origin
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAppCors(configuration, TestWebHostEnvironment.Production()));
    }

    [Fact]
    public void AppDbContext_MapsDataProtectionKeys()
    {
        using var scope = new CustomWebApplicationFactory().Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.NotNull(dbContext.Model.FindEntityType("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey"));
    }

    private static ServiceProvider BuildIdentityServiceProvider(string environmentName)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "Test.Jwt.Key.For.Runtime.Configuration.Tests.AtLeast64Characters.Long",
                ["Jwt:Issuer"] = "MultiGymManagementSystem",
                ["Jwt:Audience"] = "MultiGymManagementSystem"
            })
            .Build();

        services.AddAppIdentity(configuration, TestWebHostEnvironment.WithName(environmentName));
        return services.BuildServiceProvider();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "WebApp.Tests";
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());

        public static TestWebHostEnvironment Production() => WithName(Environments.Production);

        public static TestWebHostEnvironment WithName(string environmentName) => new()
        {
            EnvironmentName = environmentName
        };
    }
}
