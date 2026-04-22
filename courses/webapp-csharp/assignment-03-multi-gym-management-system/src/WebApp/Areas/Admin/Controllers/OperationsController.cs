using System.Globalization;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OperationsController(AppDbContext dbContext, IUserContextService userContextService) : Controller
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

        var openingHours = await dbContext.OpeningHours
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Weekday)
            .Select(entity => new OpeningHoursSummaryViewModel
            {
                Weekday = entity.Weekday,
                OpensAt = entity.OpensAt,
                ClosesAt = entity.ClosesAt
            })
            .ToArrayAsync();

        var equipment = await dbContext.Equipment
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.AssetTag)
            .Select(entity => new EquipmentSummaryViewModel
            {
                AssetTag = entity.AssetTag ?? string.Empty,
                ModelName = entity.EquipmentModel!.Name.Translate(culture) ?? string.Empty,
                Status = entity.CurrentStatus
            })
            .ToArrayAsync();

        var tasks = await dbContext.MaintenanceTasks
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.DueAtUtc)
            .Select(entity => new MaintenanceSummaryViewModel
            {
                AssetTag = entity.Equipment!.AssetTag ?? string.Empty,
                TaskType = entity.TaskType,
                Status = entity.Status,
                AssignedTo = entity.AssignedStaff == null
                    ? null
                    : $"{entity.AssignedStaff.Person!.FirstName} {entity.AssignedStaff.Person.LastName}".Trim(),
                DueAtUtc = entity.DueAtUtc
            })
            .ToArrayAsync();

        return View(new AdminOperationsPageViewModel
        {
            GymCode = context.ActiveGymCode ?? string.Empty,
            OpeningHours = openingHours,
            Equipment = equipment,
            MaintenanceTasks = tasks
        });
    }
}
