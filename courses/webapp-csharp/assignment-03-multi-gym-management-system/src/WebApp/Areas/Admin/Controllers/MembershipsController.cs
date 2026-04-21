using System.Globalization;
using App.BLL.Contracts;
using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MembershipsController(AppDbContext dbContext, IUserContextService userContextService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var gymId = context.ActiveGymId.Value;
        var culture = CultureInfo.CurrentUICulture.Name;

        var packages = await dbContext.MembershipPackages
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.BasePrice)
            .Select(entity => new MembershipPackageSummaryViewModel
            {
                Name = entity.Name.Translate(culture) ?? string.Empty,
                BasePrice = entity.BasePrice,
                CurrencyCode = entity.CurrencyCode,
                IsTrainingFree = entity.IsTrainingFree,
                TrainingDiscountPercent = entity.TrainingDiscountPercent
            })
            .ToArrayAsync();

        var memberships = await dbContext.Memberships
            .Where(entity => entity.GymId == gymId && entity.Status == MembershipStatus.Active)
            .OrderBy(entity => entity.EndDate)
            .Select(entity => new ActiveMembershipSummaryViewModel
            {
                MemberName = $"{entity.Member!.Person!.FirstName} {entity.Member.Person.LastName}".Trim(),
                PackageName = entity.MembershipPackage!.Name.Translate(culture) ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status
            })
            .ToArrayAsync();

        return View(new AdminMembershipsPageViewModel
        {
            GymCode = context.ActiveGymCode ?? string.Empty,
            Packages = packages,
            ActiveMemberships = memberships
        });
    }
}
