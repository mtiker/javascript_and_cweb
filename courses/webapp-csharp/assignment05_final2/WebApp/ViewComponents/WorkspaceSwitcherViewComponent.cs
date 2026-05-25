using System.Security.Claims;
using App.BLL.Contracts.Services;
using SharedKernel;
using App.Domain.Identity;
using SharedKernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.ViewComponents;

public class WorkspaceSwitcherViewComponent(
    IWorkspaceContextService workspaceContextService,
    UserManager<AppUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Content(string.Empty);
        }

        var isSystemAdmin = HttpContext.User.IsInRole(RoleNames.SystemAdmin);
        var activeGymCode = HttpContext.User.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = HttpContext.User.FindFirstValue(AppClaimTypes.ActiveRole);
        var options = await workspaceContextService.GetSwitchOptionsAsync(user.Id, isSystemAdmin, activeGymCode);

        if (options.Gyms.Count == 0 && !isSystemAdmin)
        {
            return Content(string.Empty);
        }

        var model = new WorkspaceSwitcherViewModel
        {
            ActiveGymCode = activeGymCode,
            ActiveRole = activeRole,
            ReturnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString,
            Gyms = options.Gyms.Select(gym => new WorkspaceGymOptionViewModel
            {
                Code = gym.Code,
                Name = gym.Name
            }).ToArray(),
            RolesInActiveGym = options.RolesInActiveGym
        };

        return View(model);
    }
}
