using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class GymsController(IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        if (!(User.IsInRole(RoleNames.SystemAdmin) || User.IsInRole(RoleNames.SystemSupport) || User.IsInRole(RoleNames.SystemBilling)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return Redirect(ClientAppUrlResolver.GetRouteUrl(configuration, "/platform"));
    }
}
