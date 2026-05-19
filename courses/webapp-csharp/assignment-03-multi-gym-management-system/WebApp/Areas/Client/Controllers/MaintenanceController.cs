using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Client.Services;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class MaintenanceController(IClientMaintenancePageService maintenancePageService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await maintenancePageService.BuildIndexAsync();
        return model == null
            ? RedirectToAction("Index", "Dashboard")
            : View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await maintenancePageService.BuildDetailsAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, MaintenanceTaskStatus status, string? notes)
    {
        if (!await maintenancePageService.UpdateStatusAsync(id, status, notes))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        TempData["StatusMessage"] = "Maintenance task updated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
