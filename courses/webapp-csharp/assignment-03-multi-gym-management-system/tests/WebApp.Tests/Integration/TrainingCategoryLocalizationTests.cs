using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using App.DAL.EF;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.Identity;
using App.DTO.v1.TrainingCategories;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class TrainingCategoryLocalizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task TrainingCategoryCrud_CreateReadUpdateDelete_WorksForGymAdmin()
    {
        var client = await CreateAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createRequest = new TrainingCategoryUpsertRequest
        {
            Name = $"Phase5 Strength {suffix}",
            Description = "Initial category description"
        };

        var createResponse = await client.PostAsJsonAsync($"/api/v1/{GymCode}/training-categories", createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse>();
        Assert.NotNull(created);
        Assert.Equal(createRequest.Name, created!.Name);
        Assert.Equal(createRequest.Description, created.Description);

        var listResponse = await client.GetAsync($"/api/v1/{GymCode}/training-categories");
        listResponse.EnsureSuccessStatusCode();
        var categories = await listResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>();
        Assert.Contains(categories!, category => category.Id == created.Id && category.Name == createRequest.Name);

        var updateRequest = new TrainingCategoryUpsertRequest
        {
            Name = $"Phase5 Conditioning {suffix}",
            Description = "Updated category description"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/{GymCode}/training-categories/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(updateRequest.Name, updated.Name);
        Assert.Equal(updateRequest.Description, updated.Description);

        var deleteResponse = await client.DeleteAsync($"/api/v1/{GymCode}/training-categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/api/v1/{GymCode}/training-categories");
        afterDeleteResponse.EnsureSuccessStatusCode();
        var afterDelete = await afterDeleteResponse.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>();
        Assert.DoesNotContain(afterDelete!, category => category.Id == created.Id);
    }

    [Fact]
    public async Task TrainingCategories_AcceptLanguageEn_ReturnsEnglishLangStrValue()
    {
        var categoryId = await SeedTranslatedCategoryAsync("Phase5 English Name", "Phase5 Estonian Name");
        var client = await CreateAdminClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");

        var response = await client.GetAsync($"/api/v1/{GymCode}/training-categories");

        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>();
        var category = Assert.Single(categories!, item => item.Id == categoryId);
        Assert.Equal("Phase5 English Name", category.Name);
    }

    [Theory]
    [InlineData("et")]
    [InlineData("et-EE")]
    public async Task TrainingCategories_AcceptLanguageEt_ReturnsEstonianLangStrValue(string culture)
    {
        var categoryId = await SeedTranslatedCategoryAsync("Phase5 English Mobility", "Phase5 Estonian Mobility");
        var client = await CreateAdminClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        var response = await client.GetAsync($"/api/v1/{GymCode}/training-categories");

        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>();
        var category = Assert.Single(categories!, item => item.Id == categoryId);
        Assert.Equal("Phase5 Estonian Mobility", category.Name);
    }

    [Fact]
    public async Task TrainingCategories_MissingTranslation_FallsBackSafely()
    {
        var categoryId = await SeedCategoryWithOnlyEnglishNameAsync("Phase5 Fallback English");
        var client = await CreateAdminClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("et-EE");

        var response = await client.GetAsync($"/api/v1/{GymCode}/training-categories");

        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<TrainingCategoryResponse[]>();
        var category = Assert.Single(categories!, item => item.Id == categoryId);
        Assert.Equal("Phase5 Fallback English", category.Name);
    }

    [Fact]
    public async Task CreateTrainingCategory_InvalidName_ReturnsProblemDetails()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/{GymCode}/training-categories", new TrainingCategoryUpsertRequest
        {
            Name = "   ",
            Description = "Invalid category"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":400", body);
        Assert.Contains("name", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MvcLoginLabels_UseResxResourcesForRequestedCulture()
    {
        var englishClient = factory.CreateClient();
        englishClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        var englishResponse = await englishClient.GetAsync("/");
        englishResponse.EnsureSuccessStatusCode();
        var englishHtml = await englishResponse.Content.ReadAsStringAsync();

        Assert.Contains("Log in", englishHtml);
        Assert.Contains("Language", englishHtml);

        var estonianClient = factory.CreateClient();
        estonianClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("et-EE");
        var estonianResponse = await estonianClient.GetAsync("/");
        estonianResponse.EnsureSuccessStatusCode();
        var estonianHtml = await estonianResponse.Content.ReadAsStringAsync();

        Assert.Contains("Logi sisse", estonianHtml);
        Assert.Contains("Keel", estonianHtml);
    }

    [Fact]
    public async Task AdminMembersPage_UsesResxResourcesForRequestedCulture()
    {
        var englishClient = await CreateMvcAdminClientAsync("en");
        var englishResponse = await englishClient.GetAsync("/Admin/Members");
        englishResponse.EnsureSuccessStatusCode();
        var englishHtml = await englishResponse.Content.ReadAsStringAsync();

        Assert.Contains("Admin workspace", englishHtml);
        Assert.Contains("Member directory", englishHtml);
        Assert.Contains("Add new member", englishHtml);

        var estonianClient = await CreateMvcAdminClientAsync("et-EE");
        var estonianResponse = await estonianClient.GetAsync("/Admin/Members");
        estonianResponse.EnsureSuccessStatusCode();
        var estonianHtml = await estonianResponse.Content.ReadAsStringAsync();

        Assert.Contains("Admini t\u00f6\u00f6laud", estonianHtml);
        Assert.Contains("Liikmete kataloog", estonianHtml);
        Assert.Contains("Lisa uus liige", estonianHtml);
        Assert.DoesNotContain("Member directory", estonianHtml);
    }

    private async Task<Guid> SeedTranslatedCategoryAsync(string englishName, string estonianName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gym = dbContext.Gyms.Single(entity => entity.Code == GymCode);
        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr
            {
                ["en"] = englishName,
                ["et"] = estonianName
            },
            Description = new LangStr("Phase5 localized category", "en")
        };

        dbContext.TrainingCategories.Add(category);
        await dbContext.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> SeedCategoryWithOnlyEnglishNameAsync(string englishName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gym = dbContext.Gyms.Single(entity => entity.Code == GymCode);
        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr
            {
                ["en"] = englishName
            }
        };

        dbContext.TrainingCategories.Add(category);
        await dbContext.SaveChangesAsync();
        return category.Id;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = "admin@peakforge.local",
            Password = "GymStrong123!"
        });
        login.EnsureSuccessStatusCode();
        var payload = (await login.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private async Task<HttpClient> CreateMvcAdminClientAsync(string culture)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client);

        var loginResponse = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@peakforge.local",
            ["Password"] = "GymStrong123!",
            ["__RequestVerificationToken"] = antiForgeryToken
        }));

        loginResponse.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "Could not extract antiforgery token from the rendered login page.");
        return match.Groups[1].Value;
    }
}
