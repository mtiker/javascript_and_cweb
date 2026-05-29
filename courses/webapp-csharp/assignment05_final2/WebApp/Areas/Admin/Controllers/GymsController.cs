using SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

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

    [HttpGet]
    public IActionResult Create()
    {
        if (!User.IsInRole(RoleNames.SystemAdmin))
        {
            return Forbid();
        }

        return View(new AdminGymFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminGymFormViewModel form, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(RoleNames.SystemAdmin))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await gymsPageService.CreateAsync(form, cancellationToken);
        if (result.Status == AdminGymOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(RoleNames.SystemAdmin))
        {
            return Forbid();
        }

        var form = await gymsPageService.GetEditFormAsync(id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminGymFormViewModel form, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(RoleNames.SystemAdmin))
        {
            return Forbid();
        }

        form.Id = id;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await gymsPageService.UpdateAsync(id, form, cancellationToken);
        if (result.Status == AdminGymOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminGymOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(form);
    }
}
