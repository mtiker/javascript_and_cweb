using App.BLL.Services;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using App.DTO.v1.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Identity;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
public class AccountController(IIdentityService identityService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.RegisterAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.LoginAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> Logout(CancellationToken cancellationToken)
    {
        await identityService.LogoutAsync(cancellationToken);
        return Ok(new Message("Logged out."));
    }

    [AllowAnonymous]
    [HttpPost("renew-refresh-token")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> RenewRefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.RenewRefreshTokenAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-gym")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchGym([FromBody] SwitchGymRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.SwitchGymAsync(request, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-role")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchRole([FromBody] SwitchRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.SwitchRoleAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.ForgotPasswordAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await identityService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new Message("Password updated."));
    }
}
