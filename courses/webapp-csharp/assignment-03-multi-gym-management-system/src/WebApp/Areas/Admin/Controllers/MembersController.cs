using App.BLL.Services;
using App.Domain;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MembersController(
    IUserContextService userContextService,
    IMemberWorkflowService memberWorkflowService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode) ||
            !(context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin)))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var members = await memberWorkflowService.GetMembersAsync(context.ActiveGymCode, cancellationToken);
        var summaries = members
            .Select(member => new AdminMemberSummaryViewModel
            {
                Id = member.Id,
                MemberCode = member.MemberCode,
                FullName = member.FullName,
                Status = member.Status
            })
            .ToArray();

        return View(new AdminMembersPageViewModel
        {
            GymCode = context.ActiveGymCode,
            TotalCount = summaries.Length,
            ActiveCount = summaries.Count(member => member.Status == MemberStatus.Active),
            SuspendedCount = summaries.Count(member => member.Status == MemberStatus.Suspended),
            LeftCount = summaries.Count(member => member.Status == MemberStatus.Left),
            Members = summaries
        });
    }
}
