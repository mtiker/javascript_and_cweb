using System.Globalization;
using App.BLL.Services;
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
public class SessionsController(AppDbContext dbContext, IUserContextService userContextService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var culture = CultureInfo.CurrentUICulture.Name;
        var sessions = await dbContext.TrainingSessions
            .Include(entity => entity.Bookings)
            .Include(entity => entity.WorkShifts)
                .ThenInclude(entity => entity.Contract!)
                    .ThenInclude(entity => entity.Staff!)
                        .ThenInclude(entity => entity.Person)
            .Where(entity => entity.GymId == context.ActiveGymId.Value)
            .OrderBy(entity => entity.StartAtUtc)
            .ToListAsync();

        return View(new AdminSessionsPageViewModel
        {
            GymCode = context.ActiveGymCode ?? string.Empty,
            Sessions = sessions.Select(entity => new AdminSessionSummaryViewModel
            {
                Name = entity.Name.Translate(culture) ?? string.Empty,
                StartAtUtc = entity.StartAtUtc,
                EndAtUtc = entity.EndAtUtc,
                Capacity = entity.Capacity,
                BookingCount = entity.Bookings.Count,
                Status = entity.Status,
                TrainerNames = string.Join(", ",
                    entity.WorkShifts
                        .Where(shift => shift.ShiftType == ShiftType.Training && shift.Contract?.Staff?.Person != null)
                        .Select(shift => $"{shift.Contract!.Staff!.Person!.FirstName} {shift.Contract.Staff.Person.LastName}".Trim())
                        .Distinct())
            }).ToArray()
        });
    }
}
