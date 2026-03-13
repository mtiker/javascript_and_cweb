using System.Net.Http.Json;
using App.DAL.EF;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Mvc.Testing;
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
}
