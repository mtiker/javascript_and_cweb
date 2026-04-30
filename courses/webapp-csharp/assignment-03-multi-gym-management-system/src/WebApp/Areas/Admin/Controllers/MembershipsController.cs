using App.BLL.Services;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MembershipsController(
    IUserContextService userContextService,
    IMembershipPackageService membershipPackageService,
    IMembershipService membershipService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var gymCode = context.ActiveGymCode ?? string.Empty;
        var packages = (await membershipPackageService.GetPackagesAsync(gymCode))
            .OrderBy(package => package.BasePrice)
            .Select(package => new MembershipPackageSummaryViewModel
            {
                Name = package.Name,
                BasePrice = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                IsTrainingFree = package.IsTrainingFree,
                TrainingDiscountPercent = package.TrainingDiscountPercent
            })
            .ToArray();

        var activeMemberships = (await membershipService.GetActiveMembershipSummariesAsync(gymCode))
            .Select(membership => new ActiveMembershipSummaryViewModel
            {
                MemberName = membership.MemberName,
                PackageName = membership.PackageName,
                StartDate = membership.StartDate,
                EndDate = membership.EndDate,
                Status = membership.Status
            })
            .ToArray();

        return View(new AdminMembershipsPageViewModel
        {
            GymCode = gymCode,
            Packages = packages,
            ActiveMemberships = activeMemberships
        });
    }
}
