using App.BLL.Contracts.Services;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OperationsController(
    IUserContextService userContextService,
    IAdminOperationsPageService operationsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(await operationsPageService.BuildAsync(
            context.ActiveGymId.Value,
            context.ActiveGymCode ?? string.Empty,
            cancellationToken));
    }
}
