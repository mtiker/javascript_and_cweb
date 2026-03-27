using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.DAL.EF.Tenant;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestIdentitySeed
{
    [Fact]
    public async Task SeedIdentityAsync_ResetsExistingSeedUserPassword_WhenEnabled()
    {
        await using var serviceProvider = BuildServiceProvider($"identity-seed-{Guid.NewGuid():N}");

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            await AppDataInit.SeedIdentityAsync(userManager, roleManager);

            var seededUser = await userManager.FindByEmailAsync("sysadmin@dental-saas.local");
            Assert.NotNull(seededUser);

            var resetResult = await userManager.RemovePasswordAsync(seededUser!);
            Assert.True(resetResult.Succeeded);

            var addResult = await userManager.AddPasswordAsync(seededUser, "Different.Pass.123!");
            Assert.True(addResult.Succeeded);

            Assert.False(await userManager.CheckPasswordAsync(seededUser, InitialData.DefaultPassword));
            Assert.True(await userManager.CheckPasswordAsync(seededUser, "Different.Pass.123!"));
        }

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            await AppDataInit.SeedIdentityAsync(userManager, roleManager, resetSeedUserPasswords: true);

            var seededUser = await userManager.FindByEmailAsync("sysadmin@dental-saas.local");
            Assert.NotNull(seededUser);
            Assert.True(await userManager.CheckPasswordAsync(seededUser!, InitialData.DefaultPassword));
            Assert.True(await userManager.IsInRoleAsync(seededUser, App.Domain.RoleNames.SystemAdmin));
        }
    }

    private static ServiceProvider BuildServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddSingleton<ITenantProvider, TestTenantProvider>();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }
}
