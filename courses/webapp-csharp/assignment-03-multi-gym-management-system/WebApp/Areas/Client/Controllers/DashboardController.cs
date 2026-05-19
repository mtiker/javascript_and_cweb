using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Client.Services;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class DashboardController(IClientDashboardPageService dashboardPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await dashboardPageService.BuildAsync(cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        return View(viewModel);
    }
}
