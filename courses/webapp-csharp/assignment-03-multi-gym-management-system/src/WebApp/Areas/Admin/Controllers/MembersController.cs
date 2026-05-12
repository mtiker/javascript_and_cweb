using App.BLL.Services;
using App.Domain;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MembersController(
    IUserContextService userContextService,
    IAdminMembersPageService membersPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var viewModel = await membersPageService.BuildIndexAsync(gymCode, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(new AdminMemberFormViewModel
        {
            GymCode = gymCode,
            Status = MemberStatus.Active
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminMemberFormViewModel form, CancellationToken cancellationToken)
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

        var result = await membersPageService.CreateAsync(gymCode, form, cancellationToken);
        return result.Status switch
        {
            AdminMemberOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminMemberOperationStatus.NotFound => NotFound(),
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

        var form = await membersPageService.GetEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminMemberFormViewModel form, CancellationToken cancellationToken)
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

        var result = await membersPageService.UpdateAsync(gymCode, id, form, cancellationToken);
        return result.Status switch
        {
            AdminMemberOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminMemberOperationStatus.NotFound => NotFound(),
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

        var view = await membersPageService.GetDeleteViewAsync(gymCode, id, cancellationToken);
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

        var result = await membersPageService.DeleteAsync(gymCode, id, cancellationToken);
        return result.Status switch
        {
            AdminMemberOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminMemberOperationStatus.NotFound => NotFound(),
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

    private IActionResult RenderWithErrors(AdminMemberFormViewModel form, IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(form);
    }
}
