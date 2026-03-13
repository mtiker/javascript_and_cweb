using System.Security.Claims;
using App.DAL.EF;
using App.Domain.Identity;
using App.DTO.v1;
using App.DTO.v1.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Identity;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiController]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    ILogger<AccountController> logger,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JWTResponse>> Register([FromBody] Register registerModel, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(registerModel.Email);
        if (existingUser != null)
        {
            return BadRequest(new Message("User already registered."));
        }

        var user = new AppUser
        {
            Email = registerModel.Email.Trim(),
            UserName = registerModel.Email.Trim(),
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, registerModel.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(error => error.Description).ToArray();
            return BadRequest(new Message(errors));
        }

        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);
        var jwt = jwtTokenService.GenerateToken(user, [], null, null, null);

        return Ok(new JWTResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken,
            ExpiresInSeconds = jwtTokenService.ExpiresInSeconds
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JWTResponse>> Login([FromBody] Login loginModel, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(loginModel.Email.Trim());
        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(400, 1200), cancellationToken);
            return NotFound(new Message("User/Password problem."));
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
        if (!passwordResult.Succeeded)
        {
            await Task.Delay(Random.Shared.Next(400, 1200), cancellationToken);
            return NotFound(new Message("User/Password problem."));
        }

        var systemRoles = await userManager.GetRolesAsync(user);

        var companyRole = await dbContext.AppUserRoles
            .AsNoTracking()
            .Where(entity => entity.AppUserId == user.Id && entity.IsActive)
            .OrderBy(entity => entity.AssignedAtUtc)
            .Select(entity => new { entity.CompanyId, entity.RoleName })
            .FirstOrDefaultAsync(cancellationToken);

        Guid? activeCompanyId = null;
        string? activeCompanySlug = null;
        string? activeCompanyRole = null;

        if (companyRole != null)
        {
            activeCompanyId = companyRole.CompanyId;
            activeCompanyRole = companyRole.RoleName;

            activeCompanySlug = await dbContext.Companies
                .AsNoTracking()
                .Where(entity => entity.Id == companyRole.CompanyId)
                .Select(entity => entity.Slug)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var jwt = jwtTokenService.GenerateToken(user, systemRoles, activeCompanyId, activeCompanySlug, activeCompanyRole);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        logger.LogInformation("User {Email} logged in.", user.Email);

        return Ok(new JWTResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken,
            ExpiresInSeconds = jwtTokenService.ExpiresInSeconds,
            ActiveCompanyId = activeCompanyId,
            ActiveCompanySlug = activeCompanySlug,
            ActiveCompanyRole = activeCompanyRole
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        string? resetToken = null;

        if (user != null)
        {
            var generatedToken = await userManager.GeneratePasswordResetTokenAsync(user);
            if (environment.IsDevelopment())
            {
                resetToken = generatedToken;
            }

            logger.LogInformation("Password reset requested for {Email}.", email);
        }

        return Ok(new ForgotPasswordResponse
        {
            Message = "If account exists, password reset instructions are available.",
            ResetToken = resetToken
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Message>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return BadRequest(new Message("Invalid reset token or email."));
        }

        var result = await userManager.ResetPasswordAsync(user, request.ResetToken.Trim(), request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToArray();
            return BadRequest(new Message(errors));
        }

        var tokens = await dbContext.RefreshTokens
            .Where(entity => entity.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (tokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(tokens);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new Message("Password has been reset."));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JWTResponse>> SwitchCompany([FromBody] SwitchCompanyRequest request, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        if (userId == Guid.Empty)
        {
            return BadRequest(new Message("Could not resolve user id from token."));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return BadRequest(new Message("User was not found."));
        }

        var roleLink = await dbContext.AppUserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.AppUserId == userId && entity.IsActive)
            .Join(
                dbContext.Companies.IgnoreQueryFilters().AsNoTracking().Where(entity => entity.IsActive),
                role => role.CompanyId,
                company => company.Id,
                (role, company) => new
                {
                    company.Id,
                    company.Slug,
                    role.RoleName
                })
            .SingleOrDefaultAsync(entity => entity.Slug == request.CompanySlug.Trim().ToLowerInvariant(), cancellationToken);

        if (roleLink == null)
        {
            return BadRequest(new Message("Requested company membership was not found."));
        }

        var systemRoles = await userManager.GetRolesAsync(user);
        var jwt = jwtTokenService.GenerateToken(user, systemRoles, roleLink.Id, roleLink.Slug, roleLink.RoleName);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        return Ok(new JWTResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken,
            ExpiresInSeconds = jwtTokenService.ExpiresInSeconds,
            ActiveCompanyId = roleLink.Id,
            ActiveCompanySlug = roleLink.Slug,
            ActiveCompanyRole = roleLink.RoleName
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenModel tokenModel, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        if (userId == Guid.Empty)
        {
            return Ok(new { tokenDeleteCount = 0 });
        }

        var tokens = await dbContext.RefreshTokens
            .Where(entity =>
                entity.UserId == userId &&
                (entity.RefreshToken == tokenModel.RefreshToken || entity.PreviousRefreshToken == tokenModel.RefreshToken))
            .ToListAsync(cancellationToken);

        dbContext.RefreshTokens.RemoveRange(tokens);
        var deleted = await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { tokenDeleteCount = deleted });
    }

    [HttpPost]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JWTResponse>> RenewRefreshToken([FromBody] RefreshTokenModel model, CancellationToken cancellationToken)
    {
        if (!jwtTokenService.TryReadPrincipalWithoutLifetimeValidation(model.Jwt, out var principal) || principal == null)
        {
            return BadRequest(new Message("Invalid jwt token."));
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new Message("Invalid token subject."));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return BadRequest(new Message("User was not found."));
        }

        var refreshToken = await dbContext.RefreshTokens
            .Where(entity =>
                entity.UserId == userId &&
                entity.RefreshToken == model.RefreshToken &&
                entity.Expiration > DateTime.UtcNow)
            .SingleOrDefaultAsync(cancellationToken);

        if (refreshToken == null)
        {
            return BadRequest(new Message("Refresh token is invalid or expired."));
        }

        refreshToken.PreviousRefreshToken = refreshToken.RefreshToken;
        refreshToken.PreviousExpiration = DateTime.UtcNow.AddMinutes(1);
        refreshToken.RefreshToken = Guid.NewGuid().ToString("N");
        refreshToken.Expiration = DateTime.UtcNow.AddSeconds(jwtTokenService.RefreshTokenExpiresInSeconds);

        await dbContext.SaveChangesAsync(cancellationToken);

        var companyIdClaim = principal.FindFirstValue("companyId");
        var companySlug = principal.FindFirstValue("companySlug");
        var companyRole = principal.FindFirstValue("companyRole");

        Guid? companyId = Guid.TryParse(companyIdClaim, out var parsedCompanyId) ? parsedCompanyId : null;

        var systemRoles = await userManager.GetRolesAsync(user);
        var jwt = jwtTokenService.GenerateToken(user, systemRoles, companyId, companySlug, companyRole);

        return Ok(new JWTResponse
        {
            Jwt = jwt,
            RefreshToken = refreshToken.RefreshToken,
            ExpiresInSeconds = jwtTokenService.ExpiresInSeconds,
            ActiveCompanyId = companyId,
            ActiveCompanySlug = companySlug,
            ActiveCompanyRole = companyRole
        });
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var refreshToken = new AppRefreshToken
        {
            UserId = userId,
            Expiration = DateTime.UtcNow.AddSeconds(jwtTokenService.RefreshTokenExpiresInSeconds)
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken.RefreshToken;
    }
}
