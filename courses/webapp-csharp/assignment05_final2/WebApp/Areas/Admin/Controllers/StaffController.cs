using App.BLL.Contracts.Services;
using SharedKernel;
using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class StaffController(
    IUserContextService userContextService,
    IAdminStaffPageService staffPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var viewModel = await staffPageService.BuildIndexAsync(gymCode, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(new AdminStaffFormViewModel
        {
            GymCode = gymCode,
            Status = StaffStatus.Active
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminStaffFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await staffPageService.CreateAsync(gymCode, form, cancellationToken);
        return result.Status switch
        {
            AdminStaffOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminStaffOperationStatus.NotFound => NotFound(),
            _ => RenderWithErrors(form, result.Errors)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var form = await staffPageService.GetEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminStaffFormViewModel form, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        form.GymCode = gymCode;
        form.Id = id;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await staffPageService.UpdateAsync(gymCode, id, form, cancellationToken);
        return result.Status switch
        {
            AdminStaffOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminStaffOperationStatus.NotFound => NotFound(),
            _ => RenderWithErrors(form, result.Errors)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var view = await staffPageService.GetDeleteViewAsync(gymCode, id, cancellationToken);
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

        var result = await staffPageService.DeleteAsync(gymCode, id, cancellationToken);
        return result.Status switch
        {
            AdminStaffOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminStaffOperationStatus.NotFound => NotFound(),
            _ => RedirectToAction(nameof(Index))
        };
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

    private IActionResult RenderWithErrors(AdminStaffFormViewModel form, IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(form);
    }
}
