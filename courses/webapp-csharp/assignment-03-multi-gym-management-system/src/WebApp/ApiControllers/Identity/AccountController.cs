using App.BLL.Contracts;
using App.DTO.v1;
using App.DTO.v1.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Identity;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
public class AccountController(IIdentityService identityService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Register([FromBody] RegisterRequest request)
    {
        return Ok(await identityService.RegisterAsync(request));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> Login([FromBody] LoginRequest request)
    {
        return Ok(await identityService.LoginAsync(request));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<ActionResult<Message>> Logout()
    {
        await identityService.LogoutAsync();
        return Ok(new Message("Logged out."));
    }

    [AllowAnonymous]
    [HttpPost("renew-refresh-token")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> RenewRefreshToken([FromBody] RefreshTokenRequest request)
    {
        return Ok(await identityService.RenewRefreshTokenAsync(request));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-gym")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchGym([FromBody] SwitchGymRequest request)
    {
        return Ok(await identityService.SwitchGymAsync(request));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-role")]
    [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JwtResponse>> SwitchRole([FromBody] SwitchRoleRequest request)
    {
        return Ok(await identityService.SwitchRoleAsync(request));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        return Ok(await identityService.ForgotPasswordAsync(request));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<Message>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await identityService.ResetPasswordAsync(request);
        return Ok(new Message("Password updated."));
    }
}
