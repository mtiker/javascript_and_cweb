using System.Security.Claims;
using System.Text.Json;
using App.BLL.Contracts.Impersonation;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using App.DTO.v1.System;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.System;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]/[action]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.SystemAdmin)]
public class ImpersonationController(
    IImpersonationService impersonationService,
    IJwtTokenService jwtTokenService,
    UserManager<AppUser> userManager,
    AppDbContext dbContext,
    ILogger<ImpersonationController> logger)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StartImpersonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StartImpersonationResponse>> Start(
        [FromBody] StartImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        var context = await impersonationService.StartAsync(
            User.UserId(),
            new StartImpersonationCommand(
                request.TargetUserEmail,
                request.CompanySlug,
                request.Reason),
            cancellationToken);

        var targetUser = await userManager.FindByIdAsync(context.TargetUserId.ToString());
        if (targetUser == null)
        {
            return NotFound();
        }

        var targetSystemRoles = await userManager.GetRolesAsync(targetUser);
        var extraClaims = new[]
        {
            new Claim("impersonatedByUserId", context.ImpersonatedByUserId.ToString()),
            new Claim("impersonationReason", context.Reason),
            new Claim("isImpersonated", "true")
        };

        var jwt = jwtTokenService.GenerateToken(
            targetUser,
            targetSystemRoles,
            context.ActiveCompanyId,
            context.ActiveCompanySlug,
            context.ActiveCompanyRole,
            extraClaims);

        var refreshToken = new AppRefreshToken
        {
            UserId = context.TargetUserId,
            Expiration = DateTime.UtcNow.AddSeconds(jwtTokenService.RefreshTokenExpiresInSeconds)
        };

        dbContext.RefreshTokens.Add(refreshToken);

        dbContext.AuditLogs.Add(new AuditLog
        {
            CompanyId = context.ActiveCompanyId,
            ActorUserId = context.ImpersonatedByUserId,
            EntityName = "ImpersonationSession",
            EntityId = context.TargetUserId,
            Action = "ImpersonationStart",
            ChangedAtUtc = DateTime.UtcNow,
            ChangesJson = JsonSerializer.Serialize(new
            {
                context.ImpersonatedByUserId,
                context.TargetUserId,
                context.TargetUserEmail,
                context.ActiveCompanySlug,
                context.ActiveCompanyRole,
                context.Reason
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "System admin {ActorUserId} started impersonation for target user {TargetUserId} in company {CompanySlug}",
            context.ImpersonatedByUserId,
            context.TargetUserId,
            context.ActiveCompanySlug);

        return Ok(new StartImpersonationResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken.RefreshToken,
            ExpiresInSeconds = jwtTokenService.ExpiresInSeconds,
            ActiveCompanyId = context.ActiveCompanyId,
            ActiveCompanySlug = context.ActiveCompanySlug,
            ActiveCompanyRole = context.ActiveCompanyRole,
            ImpersonatedByUserId = context.ImpersonatedByUserId,
            ImpersonationReason = context.Reason,
            TargetUserId = context.TargetUserId,
            TargetUserEmail = context.TargetUserEmail
        });
    }
}
