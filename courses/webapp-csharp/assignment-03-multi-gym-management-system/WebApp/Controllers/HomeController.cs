using System.Security.Claims;
using App.Domain;
using App.Domain.Security;
using App.BLL.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers;

public class HomeController(
    SignInManager<App.Domain.Identity.AppUser> signInManager,
    UserManager<App.Domain.Identity.AppUser> userManager,
    IWorkspaceContextService workspaceContextService) : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "et-EE",
        "et",
        "en",
        "en-US"
    };

    [AllowAnonymous]
    [HttpGet("/")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(RedirectToWorkspace));
        }

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpGet("/login")]
    public IActionResult Login()
    {
        return View("Index", new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View("Index", model);
        }

        await signInManager.SignOutAsync();
        await signInManager.SignInWithClaimsAsync(user, false, await BuildClaimsAsync(user));

        return RedirectToAction(nameof(RedirectToWorkspace));
    }

    [Authorize]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet("/workspace")]
    public IActionResult RedirectToWorkspace()
    {
        if (User.IsInRole(RoleNames.SystemAdmin) ||
            User.IsInRole(RoleNames.GymOwner) ||
            User.IsInRole(RoleNames.GymAdmin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Client" });
    }

    [AllowAnonymous]
    [HttpPost("/set-culture")]
    [ValidateAntiForgeryToken]
    public IActionResult SetCulture(string culture, string? returnUrl)
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = "et-EE";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [AllowAnonymous]
    [Route("/Home/Error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    [Authorize]
    [HttpPost("/switch-gym")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchGym(string gymCode, string? returnUrl)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var targetLink = await workspaceContextService.FindUserGymLinkAsync(user.Id, gymCode);

        if (targetLink == null && User.IsInRole(RoleNames.SystemAdmin))
        {
            targetLink = await workspaceContextService.BuildSystemAdminGymRoleAsync(user.Id, gymCode, RoleNames.GymOwner);
        }

        if (targetLink == null)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        await RefreshSignInAsync(user, targetLink);
        return LocalRedirect(returnUrl ?? "/workspace");
    }

    [Authorize]
    [HttpPost("/switch-role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchRole(string roleName, string? returnUrl)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var activeGymCode = User.FindFirstValue(AppClaimTypes.GymCode);
        if (string.IsNullOrWhiteSpace(activeGymCode))
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        var targetLink = await workspaceContextService.FindUserGymRoleLinkAsync(user.Id, activeGymCode, roleName);

        if (targetLink == null && User.IsInRole(RoleNames.SystemAdmin) && IsSystemAdminTenantRole(roleName))
        {
            targetLink = await workspaceContextService.BuildSystemAdminGymRoleAsync(user.Id, activeGymCode, roleName);
        }

        if (targetLink == null)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        await RefreshSignInAsync(user, targetLink);
        return LocalRedirect(returnUrl ?? "/workspace");
    }

    private async Task RefreshSignInAsync(App.Domain.Identity.AppUser user, App.Domain.Entities.AppUserGymRole? activeLink)
    {
        await signInManager.SignOutAsync();
        await signInManager.SignInWithClaimsAsync(user, false, await BuildClaimsAsync(user, activeLink));
    }

    private async Task<IEnumerable<Claim>> BuildClaimsAsync(App.Domain.Identity.AppUser user, App.Domain.Entities.AppUserGymRole? activeLink = null)
    {
        var claims = new List<Claim>();
        var systemRoles = (await userManager.GetRolesAsync(user)).Where(RoleNames.SystemRoles.Contains).ToList();
        claims.AddRange(systemRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        activeLink ??= await workspaceContextService.FindDefaultActiveLinkAsync(user.Id);

        if (user.PersonId.HasValue)
        {
            claims.Add(new Claim(AppClaimTypes.PersonId, user.PersonId.Value.ToString()));
        }

        if (activeLink != null)
        {
            claims.Add(new Claim(AppClaimTypes.GymId, activeLink.GymId.ToString()));
            claims.Add(new Claim(AppClaimTypes.GymCode, activeLink.Gym?.Code ?? string.Empty));
            claims.Add(new Claim(AppClaimTypes.ActiveRole, activeLink.RoleName));
            claims.Add(new Claim(ClaimTypes.Role, activeLink.RoleName));
        }

        return claims;
    }

    private static bool IsSystemAdminTenantRole(string roleName)
    {
        return string.Equals(roleName, RoleNames.GymOwner, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, RoleNames.GymAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
