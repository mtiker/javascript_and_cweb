using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using App.DTO.v1.Identity;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class IntegrationTestImpersonation : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTestImpersonation(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SystemAdmin_Can_Start_Impersonation_And_Audit_Is_Written()
    {
        var companySlug = $"imp-{Guid.NewGuid():N}"[..12];
        const string adminPassword = "Strong.Pass.123!";
        var adminEmail = $"sys-{Guid.NewGuid():N}@example.test";
        var targetEmail = $"target-{Guid.NewGuid():N}@example.test";

        Guid adminUserId;
        Guid targetUserId;
        Guid companyId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            if (!await roleManager.RoleExistsAsync(RoleNames.SystemAdmin))
            {
                var roleResult = await roleManager.CreateAsync(new AppRole { Name = RoleNames.SystemAdmin });
                Assert.True(roleResult.Succeeded);
            }

            var adminUser = new AppUser
            {
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true
            };
            var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
            Assert.True(createAdmin.Succeeded);

            var addRoleResult = await userManager.AddToRoleAsync(adminUser, RoleNames.SystemAdmin);
            Assert.True(addRoleResult.Succeeded);

            var targetUser = new AppUser
            {
                Email = targetEmail,
                UserName = targetEmail,
                EmailConfirmed = true
            };
            var createTarget = await userManager.CreateAsync(targetUser, adminPassword);
            Assert.True(createTarget.Succeeded);

            var company = new Company
            {
                Name = "Impersonation Clinic",
                Slug = companySlug,
                IsActive = true
            };
            db.Companies.Add(company);

            db.AppUserRoles.Add(new AppUserRole
            {
                AppUserId = targetUser.Id,
                CompanyId = company.Id,
                RoleName = RoleNames.CompanyEmployee,
                IsActive = true
            });

            await db.SaveChangesAsync();

            adminUserId = adminUser.Id;
            targetUserId = targetUser.Id;
            companyId = company.Id;
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = adminEmail,
            Password = adminPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(loginPayload);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);

        var impersonationResponse = await _client.PostAsJsonAsync("/api/v1/system/impersonation/start", new StartImpersonationRequest
        {
            TargetUserEmail = targetEmail,
            CompanySlug = companySlug,
            Reason = "Support session for appointment troubleshooting"
        });
        impersonationResponse.EnsureSuccessStatusCode();

        var payload = await impersonationResponse.Content.ReadFromJsonAsync<StartImpersonationResponse>();
        Assert.NotNull(payload);
        Assert.Equal(adminUserId, payload.ImpersonatedByUserId);
        Assert.Equal(targetUserId, payload.TargetUserId);
        Assert.Equal(companySlug, payload.ActiveCompanySlug);
        Assert.Equal(RoleNames.CompanyEmployee, payload.ActiveCompanyRole);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Jwt);
        Assert.Equal(adminUserId.ToString(), jwt.Claims.First(claim => claim.Type == "impersonatedByUserId").Value);
        Assert.Equal("true", jwt.Claims.First(claim => claim.Type == "isImpersonated").Value);
        Assert.Equal(companySlug, jwt.Claims.First(claim => claim.Type == "companySlug").Value);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditLog = await verifyDb.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log =>
                log.Action == "ImpersonationStart" &&
                log.ActorUserId == adminUserId &&
                log.EntityId == targetUserId &&
                log.CompanyId == companyId);

        Assert.NotNull(auditLog);
    }
}
