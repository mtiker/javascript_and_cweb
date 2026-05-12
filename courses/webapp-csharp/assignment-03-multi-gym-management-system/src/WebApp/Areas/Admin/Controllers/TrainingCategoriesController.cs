using App.BLL.Services;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.Services;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class TrainingCategoriesController(
    IUserContextService userContextService,
    IAdminTrainingCategoriesPageService trainingCategoriesPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        var viewModel = await trainingCategoriesPageService.BuildIndexAsync(gymCode, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!TryGetTenantAdminGymCode(out var gymCode, out var redirect))
        {
            return redirect!;
        }

        return View(new AdminTrainingCategoryFormViewModel
        {
            GymCode = gymCode,
            Name = string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken)
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

        var result = await trainingCategoriesPageService.CreateAsync(gymCode, form, cancellationToken);
        return result.Status switch
        {
            AdminTrainingCategoryOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminTrainingCategoryOperationStatus.NotFound => NotFound(),
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

        var form = await trainingCategoriesPageService.GetEditFormAsync(gymCode, id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken)
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

        var result = await trainingCategoriesPageService.UpdateAsync(gymCode, id, form, cancellationToken);
        return result.Status switch
        {
            AdminTrainingCategoryOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminTrainingCategoryOperationStatus.NotFound => NotFound(),
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

        var view = await trainingCategoriesPageService.GetDeleteViewAsync(gymCode, id, cancellationToken);
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

        var result = await trainingCategoriesPageService.DeleteAsync(gymCode, id, cancellationToken);
        return result.Status switch
        {
            AdminTrainingCategoryOperationStatus.Success => RedirectToAction(nameof(Index)),
            AdminTrainingCategoryOperationStatus.NotFound => NotFound(),
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

    private IActionResult RenderWithErrors(AdminTrainingCategoryFormViewModel form, IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(form);
    }
}
