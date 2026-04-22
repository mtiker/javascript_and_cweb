using App.DAL.EF;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddAppIdentity(configuration));

        Assert.Equal("Jwt:Key is missing.", exception.Message);
    }

    [Fact]
    public void AppDbContext_MapsDataProtectionKeys()
    {
        using var scope = new CustomWebApplicationFactory().Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.NotNull(dbContext.Model.FindEntityType("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey"));
    }
}
