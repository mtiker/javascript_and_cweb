using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class GymsController(AppDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!(User.IsInRole(RoleNames.SystemAdmin) || User.IsInRole(RoleNames.SystemSupport) || User.IsInRole(RoleNames.SystemBilling)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var gyms = await dbContext.Gyms.OrderBy(entity => entity.Name).ToListAsync();
        return View(gyms);
    }
}
