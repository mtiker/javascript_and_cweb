using App.BLL.Services;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OperationsController(IUserContextService userContextService, IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return Redirect(ClientAppUrlResolver.GetRouteUrl(configuration, "/maintenance"));
    }
}
