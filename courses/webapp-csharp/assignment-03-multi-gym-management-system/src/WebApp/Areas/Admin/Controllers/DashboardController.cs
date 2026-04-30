using App.BLL.Services;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController(
    IUserContextService userContextService,
    IAdminDashboardPageService dashboardPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (!(context.HasRole(RoleNames.SystemAdmin) || context.HasRole(RoleNames.SystemSupport) || context.HasRole(RoleNames.SystemBilling)
              || context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Client" });
        }

        return View(await dashboardPageService.BuildAsync(cancellationToken));
    }
}
