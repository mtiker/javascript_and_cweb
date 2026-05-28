using System.Net;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

// Failing-by-design contract tests for the Admin/Members MVC CRUD workflow.
// The production controller currently exposes only Index. These tests pin the
// expected shape of Create/Edit/Delete (GET form + POST handler) and the
// authorization/tenant-isolation contract that the workflow must satisfy.
public class AdminMembersCrudTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@peakforge.local";
    private const string MemberEmail = "member@peakforge.local";
    private const string MultiGymAdminEmail = "multigym.admin@gym.local";
    private const string Password = "GymStrong123!";
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task AdminMembers_Index_AuthorizedAdminCanOpen()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MEM-001", html);
        Assert.Contains(GymCode, html);
    }

    [Fact]
    public async Task AdminMembers_Create_AuthorizedAdminCanOpenForm()
    {
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync("/Admin/Members/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MemberCode", html);
        Assert.Contains("FirstName", html);
        Assert.Contains("LastName", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminMembers_Create_InvalidPost_ReturnsValidationErrors()
    {
        var client = await CreateMvcClientAsync(AdminEmail);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Members/Create");

        var response = await client.PostAsync("/Admin/Members/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "",
            ["LastName"] = "",
            ["MemberCode"] = "",
            ["PersonalCode"] = "",
            ["Status"] = nameof(MemberStatus.Active)
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches(
            @"(field-validation-error|input-validation-error|validation-summary-errors)",
            html);
    }

    [Fact]
    public async Task AdminMembers_Create_ValidPost_PersistsMemberForActiveGym()
    {
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, "/Admin/Members/Create");

        const string memberCode = "MEM-MVC-CRT-1";
        const string personalCode = "49911111101";

        var response = await client.PostAsync("/Admin/Members/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "MvcCreate",
            ["LastName"] = "Tester",
            ["MemberCode"] = memberCode,
            ["PersonalCode"] = personalCode,
            ["Status"] = nameof(MemberStatus.Active),
            ["Email"] = "mvc.create.tester@example.com",
            ["NewPassword"] = "Test.Password1!"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Members", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == GymCode)
            .Select(gym => gym.Id)
            .SingleAsync();

        var created = await dbContext.Members
            .Include(member => member.Person)
            .SingleOrDefaultAsync(member => member.MemberCode == memberCode && member.GymId == gymId);

        Assert.NotNull(created);
        Assert.NotNull(created!.Person);
        Assert.Equal("MvcCreate", created.Person!.FirstName);
        Assert.Equal("Tester", created.Person.LastName);
        Assert.Equal(personalCode, created.Person.PersonalCode);
    }

    [Fact]
    public async Task AdminMembers_Edit_AuthorizedAdminCanOpenForm()
    {
        var memberId = await GetSeededMemberIdAsync("MEM-001");
        var client = await CreateMvcClientAsync(AdminEmail);

        var response = await client.GetAsync($"/Admin/Members/Edit/{memberId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MEM-001", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task AdminMembers_Edit_ValidPost_UpdatesMember()
    {
        var memberId = await GetSeededMemberIdAsync("MEM-002");
        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Members/Edit/{memberId}");

        var response = await client.PostAsync($"/Admin/Members/Edit/{memberId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = memberId.ToString(),
            ["FirstName"] = "RenamedMvc",
            ["LastName"] = "Tester",
            ["MemberCode"] = "MEM-002",
            ["PersonalCode"] = "49507070007",
            ["Status"] = nameof(MemberStatus.Suspended)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Members", location, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.Members
            .Include(member => member.Person)
            .SingleAsync(member => member.Id == memberId);

        Assert.Equal("RenamedMvc", updated.Person!.FirstName);
        Assert.Equal(MemberStatus.Suspended, updated.Status);
    }

    [Fact]
    public async Task AdminMembers_Delete_RemovesSoftDeletesOrDeactivatesMember()
    {
        Guid memberId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.SingleAsync(entity => entity.Code == GymCode);

            var member = new Member
            {
                GymId = gym.Id,
                Person = new Person
                {
                    FirstName = "ToDelete",
                    LastName = "Mvc",
                    PersonalCode = "49922222202"
                },
                MemberCode = "MEM-MVC-DEL-1",
                Status = MemberStatus.Active
            };

            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync();
            memberId = member.Id;
        }

        var client = await CreateMvcClientAsync(AdminEmail, allowAutoRedirect: false);
        var token = await GetFormAntiforgeryTokenAsync(client, $"/Admin/Members/Delete/{memberId}");

        var response = await client.PostAsync($"/Admin/Members/Delete/{memberId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = memberId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.Contains("/Admin/Members", location, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var afterDelete = await verifyContext.Members
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(member => member.Id == memberId);

        // Domain behavior may be hard delete, soft delete (IsDeleted), or deactivate (Status = Left).
        var removedOrInactive =
            afterDelete is null
            || afterDelete.IsDeleted
            || afterDelete.Status == MemberStatus.Left;

        Assert.True(
            removedOrInactive,
            "After delete the member must be removed, soft-deleted, or marked Left.");

        // Whatever the storage outcome, the member must no longer surface in the active listing.
        var listResponse = await client.GetAsync("/Admin/Members");
        listResponse.EnsureSuccessStatusCode();
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("MEM-MVC-DEL-1", listHtml);
    }

    [Fact]
    public async Task AdminMembers_UnauthorizedUser_CannotAccess()
    {
        var client = await CreateMvcClientAsync(MemberEmail, allowAutoRedirect: false);

        var response = await client.GetAsync("/Admin/Members");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound,
            $"Expected redirect/403/404 for non-admin access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            var location = response.Headers.Location?.OriginalString ?? string.Empty;
            Assert.DoesNotMatch(@"/Admin/Members(?:[/?]|$)", location);
        }
    }

    [Fact]
    public async Task AdminMembers_CrossTenantMemberId_Returns403Or404()
    {
        var peakForgeMemberId = await GetSeededMemberIdAsync("MEM-001");
        var client = await CreateMvcClientAsync(MultiGymAdminEmail, allowAutoRedirect: false);

        var response = await client.GetAsync($"/Admin/Members/Edit/{peakForgeMemberId}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.Redirect,
            $"Expected 403/404/redirect for cross-tenant access but got {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Fail("Cross-tenant member edit must not return 200 OK.");
        }
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

    private async Task<Guid> GetSeededMemberIdAsync(string memberCode)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Members
            .Where(member => member.MemberCode == memberCode)
            .Select(member => member.Id)
            .SingleAsync();
    }
}
