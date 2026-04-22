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
public class DashboardController(AppDbContext dbContext, IUserContextService userContextService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!(context.HasRole(RoleNames.SystemAdmin) || context.HasRole(RoleNames.SystemSupport) || context.HasRole(RoleNames.SystemBilling)
              || context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Client" });
        }

        var viewModel = new AdminDashboardViewModel
        {
            ActiveGymCode = context.ActiveGymCode,
            ActiveRole = context.ActiveRole,
            SystemRoles = context.SystemRoles
        };

        viewModel.GymCount = await dbContext.Gyms.CountAsync();

        if (context.ActiveGymId.HasValue)
        {
            var gymId = context.ActiveGymId.Value;
            viewModel.MemberCount = await dbContext.Members.CountAsync(entity => entity.GymId == gymId);
            viewModel.SessionCount = await dbContext.TrainingSessions.CountAsync(entity => entity.GymId == gymId);
            viewModel.OpenMaintenanceTaskCount = await dbContext.MaintenanceTasks.CountAsync(entity => entity.GymId == gymId && entity.Status != App.Domain.Enums.MaintenanceTaskStatus.Done);
        }

        return View(viewModel);
    }
}
