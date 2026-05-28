using App.BLL.Contracts.Services;
using SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OperationsController(
    IUserContextService userContextService,
    IAdminOperationsPageService operationsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out var gymId, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await operationsPageService.BuildAsync(gymId, gymCode, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentCreate(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await operationsPageService.BuildEquipmentCreateFormAsync(gymCode, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EquipmentCreate(AdminEquipmentFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;

        if (!ModelState.IsValid)
        {
            await operationsPageService.PopulateEquipmentOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await operationsPageService.CreateEquipmentAsync(gymCode, form, cancellationToken);
        return await ResolveEquipmentFormResultAsync(result, gymCode, form, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentEdit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var form = await operationsPageService.GetEquipmentEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EquipmentEdit(Guid id, AdminEquipmentFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;
        form.Id = id;

        if (!ModelState.IsValid)
        {
            await operationsPageService.PopulateEquipmentOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await operationsPageService.UpdateEquipmentAsync(gymCode, id, form, cancellationToken);
        return await ResolveEquipmentFormResultAsync(result, gymCode, form, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentDelete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var view = await operationsPageService.GetEquipmentDeleteViewAsync(gymCode, id, cancellationToken);
        if (view is null)
        {
            return NotFound();
        }

        return View(view);
    }

    [HttpPost]
    [ActionName("EquipmentDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EquipmentDeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var result = await operationsPageService.DeleteEquipmentAsync(gymCode, id, cancellationToken);
        if (result.Status == AdminEquipmentOperationStatus.NotFound)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ResolveEquipmentFormResultAsync(
        AdminEquipmentOperationResult result,
        string gymCode,
        AdminEquipmentFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (result.Status == AdminEquipmentOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminEquipmentOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await operationsPageService.PopulateEquipmentOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> TaskCreate(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await operationsPageService.BuildTaskCreateFormAsync(gymCode, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskCreate(AdminMaintenanceTaskFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;

        if (!ModelState.IsValid)
        {
            await operationsPageService.PopulateTaskCreateOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await operationsPageService.CreateTaskAsync(gymCode, form, cancellationToken);
        if (result.Status == AdminEquipmentOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminEquipmentOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await operationsPageService.PopulateTaskCreateOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> TaskEdit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var form = await operationsPageService.GetTaskEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskEdit(Guid id, AdminMaintenanceTaskEditFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;
        form.Id = id;

        if (!ModelState.IsValid)
        {
            await operationsPageService.PopulateTaskEditOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await operationsPageService.UpdateTaskAsync(gymCode, id, form, cancellationToken);
        if (result.Status == AdminEquipmentOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminEquipmentOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await operationsPageService.PopulateTaskEditOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> TaskDelete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var view = await operationsPageService.GetTaskDeleteViewAsync(gymCode, id, cancellationToken);
        if (view is null)
        {
            return NotFound();
        }

        return View(view);
    }

    [HttpPost]
    [ActionName("TaskDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskDeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var result = await operationsPageService.DeleteTaskAsync(gymCode, id, cancellationToken);
        if (result.Status == AdminEquipmentOperationStatus.NotFound)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetTenantAdminContext(out Guid gymId, out string gymCode, out IActionResult? redirect)
    {
        var context = userContextService.GetCurrent();
        gymCode = context.ActiveGymCode ?? string.Empty;
        gymId = context.ActiveGymId ?? Guid.Empty;
        if (!context.ActiveGymId.HasValue ||
            string.IsNullOrWhiteSpace(gymCode) ||
            !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            redirect = RedirectToAction("Index", "Dashboard");
            return false;
        }

        redirect = null;
        return true;
    }
}
