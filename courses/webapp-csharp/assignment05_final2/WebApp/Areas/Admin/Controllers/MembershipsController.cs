using App.BLL.Contracts.Services;
using SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MembershipsController(
    IUserContextService userContextService,
    IAdminMembershipsPageService membershipsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await membershipsPageService.BuildIndexAsync(gymCode, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(await membershipsPageService.BuildSellFormAsync(gymCode, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminMembershipSellFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;

        if (!ModelState.IsValid)
        {
            await membershipsPageService.PopulateSellOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await membershipsPageService.SellAsync(gymCode, form, cancellationToken);
        if (result.Status == AdminMembershipOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminMembershipOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await membershipsPageService.PopulateSellOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var form = await membershipsPageService.GetEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminMembershipEditFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;
        form.Id = id;

        if (!ModelState.IsValid)
        {
            await membershipsPageService.PopulateEditOptionsAsync(gymCode, form, cancellationToken);
            return View(form);
        }

        var result = await membershipsPageService.UpdateAsync(gymCode, id, form, cancellationToken);
        if (result.Status == AdminMembershipOperationStatus.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.Status == AdminMembershipOperationStatus.NotFound)
        {
            return NotFound();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await membershipsPageService.PopulateEditOptionsAsync(gymCode, form, cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var view = await membershipsPageService.GetDeleteViewAsync(gymCode, id, cancellationToken);
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
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var result = await membershipsPageService.DeleteAsync(gymCode, id, cancellationToken);
        if (result.Status == AdminMembershipOperationStatus.NotFound)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetTenantAdminGymCode(out string gymCode, out IActionResult? redirect)
    {
        var context = userContextService.GetCurrent();
        gymCode = context.ActiveGymCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(gymCode) ||
            !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            redirect = RedirectToAction("Index", "Dashboard");
            return false;
        }

        redirect = null;
        return true;
    }
}
