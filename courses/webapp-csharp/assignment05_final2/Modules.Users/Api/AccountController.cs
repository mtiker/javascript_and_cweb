using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Users.Application.Auth;

namespace Modules.Users.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class AccountController(IUsersAuthService usersAuthService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.RegisterAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.LoginAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> Logout(CancellationToken cancellationToken)
    {
        await usersAuthService.LogoutAsync(cancellationToken);
        return Ok(new Message("Logged out."));
    }

    [AllowAnonymous]
    [HttpPost("renew-refresh-token")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> RenewRefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.RenewRefreshTokenAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-gym")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchGym([FromBody] SwitchGymRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.SwitchGymAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-role")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchRole([FromBody] SwitchRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.SwitchRoleAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await usersAuthService.ForgotPasswordAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await usersAuthService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new Message("Password updated."));
    }
}
