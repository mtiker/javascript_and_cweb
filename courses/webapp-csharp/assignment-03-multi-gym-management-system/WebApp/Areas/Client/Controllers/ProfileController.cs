using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Client.Services;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class ProfileController(IClientProfilePageService profilePageService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await profilePageService.BuildAsync();
        return model == null
            ? RedirectToAction("Index", "Dashboard")
            : View(model);
    }
}
