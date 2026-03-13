using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1.Identity;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class IntegrationTestOnboarding : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTestOnboarding(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task RegisterCompany_CreatesTenantAndOwner()
    {
        await AuthenticateAsSystemOperatorAsync();

        var request = new RegisterCompanyRequest
        {
            CompanyName = "Acme Dental",
            CompanySlug = $"acme-{Guid.NewGuid():N}"[..12],
            OwnerEmail = $"owner-{Guid.NewGuid():N}@acme.test",
            OwnerPassword = "Strong.Pass.123!",
            CountryCode = "DE"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/system/onboarding/registercompany", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RegisterCompanyResponse>();
        Assert.NotNull(payload);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == payload.CompanyId);
        Assert.NotNull(company);
        Assert.Equal(request.CompanySlug.ToLowerInvariant(), company.Slug);

        var subscription = await db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CompanyId == payload.CompanyId);

        Assert.NotNull(subscription);
        Assert.Equal("Free", subscription.Tier.ToString());

        var ownerRole = await db.AppUserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CompanyId == payload.CompanyId && entity.AppUserId == payload.OwnerUserId);

        Assert.NotNull(ownerRole);
        Assert.Equal("CompanyOwner", ownerRole.RoleName);
    }

    private async Task AuthenticateAsSystemOperatorAsync()
    {
        const string email = "sysadmin-integration@test.local";
        const string password = "Strong.Pass.123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            if (!await roleManager.RoleExistsAsync(RoleNames.SystemAdmin))
            {
                var createRole = await roleManager.CreateAsync(new AppRole { Name = RoleNames.SystemAdmin });
                Assert.True(createRole.Succeeded);
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createUser = await userManager.CreateAsync(user, password);
                Assert.True(createUser.Succeeded);
            }

            if (!await userManager.IsInRoleAsync(user, RoleNames.SystemAdmin))
            {
                var addRole = await userManager.AddToRoleAsync(user, RoleNames.SystemAdmin);
                Assert.True(addRole.Succeeded);
            }
        }

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);
    }
}
