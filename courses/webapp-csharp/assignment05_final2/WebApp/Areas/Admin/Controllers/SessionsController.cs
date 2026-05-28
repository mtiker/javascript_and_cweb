using App.BLL.Contracts.Services;
using SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class SessionsController(
    IUserContextService userContextService,
    IAdminSessionsPageService sessionsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var context = userContextService.GetCurrent();
        return View(await sessionsPageService.BuildAsync(
            context.ActiveGymId!.Value,
            gymCode,
            cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await sessionsPageService.BuildCreateFormAsync(gymCode, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminSessionFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;

        if (!ModelState.IsValid)
        {
            await sessionsPageService.PopulateOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await sessionsPageService.CreateAsync(gymCode, form, cancellationToken);
        return result.Status switch
        {
            AdminSessionOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminSessionOperationStatus.NotFound => NotFound(),
            _ => await RenderWithErrorsAsync(gymCode, form, result.Errors, cancellationToken)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var form = await sessionsPageService.GetEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminSessionFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;
        form.Id = id;

        if (!ModelState.IsValid)
        {
            await sessionsPageService.PopulateOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await sessionsPageService.UpdateAsync(gymCode, id, form, cancellationToken);
        return result.Status switch
        {
            AdminSessionOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminSessionOperationStatus.NotFound => NotFound(),
            _ => await RenderWithErrorsAsync(gymCode, form, result.Errors, cancellationToken)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var view = await sessionsPageService.GetDeleteViewAsync(gymCode, id, cancellationToken);
        if (view is null)
        {
            return NotFound();
        }

        return View(view);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminContext(out _, out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var result = await sessionsPageService.DeleteAsync(gymCode, id, cancellationToken);
        if (result.Status == AdminSessionOperationStatus.NotFound)
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

    private async Task<IActionResult> RenderWithErrorsAsync(
        string gymCode,
        AdminSessionFormViewModel form,
        IReadOnlyList<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await sessionsPageService.PopulateOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }
}
