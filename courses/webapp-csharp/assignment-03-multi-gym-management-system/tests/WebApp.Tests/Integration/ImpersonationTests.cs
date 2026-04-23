using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using App.DAL.EF;
using App.Domain.Security;
using App.DTO.v1.Identity;
using App.DTO.v1.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class ImpersonationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task StartImpersonation_WritesAuditRefreshTokenAndClaims()
    {
        Guid adminUserId;
        Guid targetUserId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminUserId = await dbContext.Users
                .Where(user => user.Email == "systemadmin@gym.local")
                .Select(user => user.Id)
                .SingleAsync();

            targetUserId = await dbContext.Users
                .Where(user => user.Email == "member@peakforge.local")
                .Select(user => user.Id)
                .SingleAsync();
        }

        var client = factory.CreateClient();
        var adminSession = await LoginAsync(client, "systemadmin@gym.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminSession.Jwt);

        const string reason = "Support review for member booking issue";
        var response = await client.PostAsJsonAsync("/api/v1/system/impersonation", new StartImpersonationRequest
        {
            UserId = targetUserId,
            GymCode = "peak-forge",
            Reason = reason
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<StartImpersonationResponse>();

        Assert.NotNull(payload);
        Assert.Equal(targetUserId, payload!.UserId);
        Assert.Equal(targetUserId, payload.TargetUserId);
        Assert.Equal(adminUserId, payload.ImpersonatedByUserId);
        Assert.Equal(reason, payload.ImpersonationReason);
        Assert.Equal("peak-forge", payload.GymCode);
        Assert.False(string.IsNullOrWhiteSpace(payload.ActiveRole));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.True(payload.ExpiresInSeconds > 0);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Jwt);
        Assert.Equal(targetUserId.ToString(), jwt.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(payload.GymCode, jwt.Claims.First(claim => claim.Type == AppClaimTypes.GymCode).Value);
        Assert.Equal(payload.ActiveRole, jwt.Claims.First(claim => claim.Type == AppClaimTypes.ActiveRole).Value);
        Assert.Equal(targetUserId.ToString(), jwt.Claims.First(claim => claim.Type == AppClaimTypes.ImpersonatedUserId).Value);
        Assert.Equal(adminUserId.ToString(), jwt.Claims.First(claim => claim.Type == AppClaimTypes.ImpersonatedByUserId).Value);
        Assert.Equal(reason, jwt.Claims.First(claim => claim.Type == AppClaimTypes.ImpersonationReason).Value);
        Assert.Equal("true", jwt.Claims.First(claim => claim.Type == AppClaimTypes.IsImpersonated).Value);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var refreshTokenExists = await verifyDbContext.RefreshTokens.AnyAsync(token =>
            token.UserId == targetUserId &&
            token.RefreshToken == payload.RefreshToken);

        Assert.True(refreshTokenExists);

        var auditLog = await verifyDbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log =>
                log.Action == "ImpersonationStart" &&
                log.ActorUserId == adminUserId &&
                log.EntityId == targetUserId);

        Assert.NotNull(auditLog);
        Assert.Equal(payload.ActiveGymId, auditLog!.GymId);
        Assert.Contains(reason, auditLog.ChangesJson ?? string.Empty, StringComparison.Ordinal);
    }

    private static async Task<JwtResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();
        return (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
    }
}
