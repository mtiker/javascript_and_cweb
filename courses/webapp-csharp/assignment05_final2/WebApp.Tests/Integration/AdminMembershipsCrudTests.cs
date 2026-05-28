using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Contract tests for the Admin/Memberships MVC CRUD workflow. The admin area used
// to expose /Admin/Memberships as a read-only list; these tests pin the in-area
// sell/edit/delete flow backed by the membership service, with tenant + role gating.
public class AdminMembershipsCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminMemberships_Index_AuthorizedAdminSeesSellLink()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Memberships");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("/Admin/Memberships/Create", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminMemberships_Create_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Memberships/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MemberId", html);
        Assert.Contains("MembershipPackageId", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminMemberships_Create_ValidPost_SellsMembershipForActiveGym()
    {
        var memberId = await CreateScratchMemberAsync("MVC-MS-CRT");
        var packageId = await GetSeededPackageIdAsync();
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Memberships/Create");

        var response = await client.PostAsync("/Admin/Memberships/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["MemberId"] = memberId.ToString(),
            ["MembershipPackageId"] = packageId.ToString(),
            ["RequestedStartDate"] = "2026-06-15",
            ["PaymentReference"] = "MVC-PAY-1"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Memberships", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await dbContext.Memberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.MemberId == memberId && m.MembershipPackageId == packageId);

        Assert.NotNull(membership);
    }

    [Fact]
    public async Task AdminMemberships_Edit_ValidPost_UpdatesMembership()
    {
        var memberId = await CreateScratchMemberAsync("MVC-MS-EDIT");
        var packageId = await GetSeededPackageIdAsync();
        var membershipId = await CreateScratchMembershipAsync(memberId, packageId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Memberships/Edit/{membershipId}");

        var response = await client.PostAsync($"/Admin/Memberships/Edit/{membershipId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = membershipId.ToString(),
            ["MembershipPackageId"] = packageId.ToString(),
            ["StartDate"] = "2026-07-01",
            ["EndDate"] = "2026-08-01",
            ["Status"] = nameof(MembershipStatus.Active)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.Memberships
            .IgnoreQueryFilters()
            .SingleAsync(m => m.Id == membershipId);

        Assert.Equal(new DateOnly(2026, 7, 1), updated.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 1), updated.EndDate);
    }

    [Fact]
    public async Task AdminMemberships_Delete_RemovesMembership()
    {
        var memberId = await CreateScratchMemberAsync("MVC-MS-DEL");
        var packageId = await GetSeededPackageIdAsync();
        var membershipId = await CreateScratchMembershipAsync(memberId, packageId);
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Memberships/Delete/{membershipId}");

        var response = await client.PostAsync($"/Admin/Memberships/Delete/{membershipId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = membershipId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await dbContext.Memberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.Id == membershipId);

        var removedOrInactive = afterDelete is null || afterDelete.IsDeleted;
        Assert.True(removedOrInactive, "After delete the membership must be removed or soft-deleted.");
    }

    [Fact]
    public async Task AdminMemberships_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/Memberships/Create");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");
    }

    private async Task<Guid> CreateScratchMemberAsync(string tag)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var member = new Member
        {
            GymId = gymId,
            Person = new Person { FirstName = tag, LastName = "Scratch" },
            MemberCode = $"{tag}-{Guid.NewGuid():N}"[..14],
            Status = MemberStatus.Active
        };

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member.Id;
    }

    private async Task<Guid> CreateScratchMembershipAsync(Guid memberId, Guid packageId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var membership = new Membership
        {
            GymId = gymId,
            MemberId = memberId,
            MembershipPackageId = packageId,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 7, 1),
            PriceAtPurchase = 49m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();
        return membership.Id;
    }

    private async Task<Guid> GetSeededPackageIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        return await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .Where(package => package.GymId == gymId)
            .Select(package => package.Id)
            .FirstAsync();
    }

    private async Task<HttpClient> CreateMvcClientAsync(string email, bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
        var token = await GetFormAntiforgeryTokenAsync(client, "/");

        var loginResponse = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = Password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.True(
            loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == HttpStatusCode.Redirect,
            $"Login failed for {email} with status {(int)loginResponse.StatusCode}.");

        return client;
    }

    private static async Task<string> GetFormAntiforgeryTokenAsync(HttpClient client, string formPath)
    {
        var response = await client.GetAsync(formPath);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Could not load form page '{formPath}' to extract antiforgery token (status {(int)response.StatusCode}).");

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, $"Could not extract antiforgery token from '{formPath}'.");
        return match.Groups[1].Value;
    }
}
