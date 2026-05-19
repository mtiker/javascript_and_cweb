using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class GymsController(IAdminGymsPageService gymsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!User.IsInRole(RoleNames.SystemAdmin))
        {
            return Forbid();
        }

        return View(await gymsPageService.BuildAsync(cancellationToken));
    }
}
