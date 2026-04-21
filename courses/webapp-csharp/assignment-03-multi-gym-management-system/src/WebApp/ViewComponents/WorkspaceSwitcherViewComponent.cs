using System.Security.Claims;
using App.DAL.EF;
using App.Domain.Identity;
using App.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.ViewComponents;

public class WorkspaceSwitcherViewComponent(
    AppDbContext dbContext,
    UserManager<AppUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Content(string.Empty);
        }

        var links = await dbContext.AppUserGymRoles
            .Include(link => link.Gym)
            .Where(link => link.AppUserId == user.Id && link.IsActive)
            .OrderBy(link => link.Gym!.Name)
            .ThenBy(link => link.RoleName)
            .ToListAsync();

        if (links.Count == 0)
        {
            return Content(string.Empty);
        }

        var activeGymCode = HttpContext.User.FindFirstValue(AppClaimTypes.GymCode);
        var activeRole = HttpContext.User.FindFirstValue(AppClaimTypes.ActiveRole);

        var model = new WorkspaceSwitcherViewModel
        {
            ActiveGymCode = activeGymCode,
            ActiveRole = activeRole,
            ReturnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString,
            Gyms = links
                .Where(link => link.Gym != null)
                .GroupBy(link => link.Gym!.Code)
                .Select(group => new WorkspaceGymOptionViewModel
                {
                    Code = group.Key,
                    Name = group.First().Gym!.Name
                })
                .ToArray(),
            RolesInActiveGym = links
                .Where(link => link.Gym?.Code == activeGymCode)
                .Select(link => link.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

        return View(model);
    }
}
