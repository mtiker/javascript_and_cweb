using System.Net.Http.Json;
using System.Net.Http.Headers;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1.Identity;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class IntegrationTestIdentity : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTestIdentity(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_Then_Login_ReturnsJwtAndRefreshToken()
    {
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var password = "Strong.Pass.123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/account/register", new Register
        {
            Email = email,
            Password = password
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<JWTResponse>();

        Assert.NotNull(registerPayload);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();

        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Jwt));
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.RefreshToken));
    }

    [Fact]
    public async Task SwitchRole_ReturnsJwtWithRequestedCompanyRole()
    {
        var slug = $"roles-{Guid.NewGuid():N}"[..12];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@roles.test";
        const string ownerPassword = "Strong.Pass.123!";

        await AuthenticateAsSystemAdminAsync();

        var onboarding = await _client.PostAsJsonAsync("/api/v1/system/onboarding/registercompany", new RegisterCompanyRequest
        {
            CompanyName = "Role Clinic",
            CompanySlug = slug,
            OwnerEmail = ownerEmail,
            OwnerPassword = ownerPassword,
            CountryCode = "EE"
        });
        onboarding.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.IgnoreQueryFilters().SingleAsync(entity => entity.Slug == slug);
            var owner = await db.Users.SingleAsync(entity => entity.Email == ownerEmail);

            db.AppUserRoles.Add(new AppUserRole
            {
                AppUserId = owner.Id,
                CompanyId = company.Id,
                RoleName = RoleNames.CompanyManager,
                IsActive = true,
                AssignedAtUtc = DateTime.UtcNow.AddMinutes(1)
            });

            await db.SaveChangesAsync();
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = ownerEmail,
            Password = ownerPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(loginPayload);
        Assert.Equal(RoleNames.CompanyOwner, loginPayload.ActiveCompanyRole);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var switchRoleResponse = await _client.PostAsJsonAsync("/api/v1/account/switchrole", new SwitchRoleRequest
        {
            RoleName = RoleNames.CompanyManager
        });
        switchRoleResponse.EnsureSuccessStatusCode();

        var switchRolePayload = await switchRoleResponse.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(switchRolePayload);
        Assert.Equal(slug, switchRolePayload.ActiveCompanySlug);
        Assert.Equal(RoleNames.CompanyManager, switchRolePayload.ActiveCompanyRole);
    }

    private async Task AuthenticateAsSystemAdminAsync()
    {
        var email = $"sysadmin-{Guid.NewGuid():N}@identity.test";
        const string password = "Strong.Pass.123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/account/register", new Register
        {
            Email = email,
            Password = password
        });
        registerResponse.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(entity => entity.Email == email);
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();

            var roleExists = await db.Roles.AnyAsync(entity => entity.Name == RoleNames.SystemAdmin);
            if (!roleExists)
            {
                db.Roles.Add(new App.Domain.Identity.AppRole { Name = RoleNames.SystemAdmin });
                await db.SaveChangesAsync();
            }

            var userRoleExists = await db.UserRoles.AnyAsync(entity => entity.UserId == user.Id);
            if (!userRoleExists)
            {
                var roleId = await db.Roles.Where(entity => entity.Name == RoleNames.SystemAdmin).Select(entity => entity.Id).SingleAsync();
                db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
                await db.SaveChangesAsync();
            }
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var payload = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(payload);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
    }
}
