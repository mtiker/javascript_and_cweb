using App.BLL.Contracts;
using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class MaintenanceController(
    AppDbContext dbContext,
    IUserContextService userContextService,
    App.BLL.Contracts.IAuthorizationService authorizationService,
    IMaintenanceWorkflowService maintenanceWorkflowService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var staff = await authorizationService.GetCurrentStaffAsync(context.ActiveGymId.Value);
        var tasks = Array.Empty<MaintenanceTaskResponse>();

        if (staff != null && (context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin) || context.HasRole(RoleNames.Caretaker)))
        {
            tasks = (await maintenanceWorkflowService.GetMaintenanceTasksAsync(context.ActiveGymCode))
                .Where(entity => entity.AssignedStaffId == staff.Id)
                .OrderBy(entity => entity.DueAtUtc)
                .ToArray();
        }

        return View(new MaintenancePageViewModel
        {
            GymCode = context.ActiveGymCode,
            Tasks = tasks
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var task = (await maintenanceWorkflowService.GetMaintenanceTasksAsync(context.ActiveGymCode))
            .FirstOrDefault(entity => entity.Id == id);
        if (task == null)
        {
            return NotFound();
        }

        var equipmentLabel = await dbContext.Equipment
            .Where(entity => entity.Id == task.EquipmentId)
            .Select(entity => entity.AssetTag ?? entity.SerialNumber ?? entity.Id.ToString())
            .FirstOrDefaultAsync() ?? task.EquipmentId.ToString();

        return View(new MaintenanceTaskDetailPageViewModel
        {
            GymCode = context.ActiveGymCode,
            Task = task,
            EquipmentLabel = equipmentLabel,
            NextStatus = task.Status == MaintenanceTaskStatus.Open
                ? MaintenanceTaskStatus.InProgress
                : MaintenanceTaskStatus.Done,
            Notes = task.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, MaintenanceTaskStatus status, string? notes)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        await maintenanceWorkflowService.UpdateTaskStatusAsync(context.ActiveGymCode, id, new MaintenanceStatusUpdateRequest
        {
            Status = status,
            Notes = notes
        });
        TempData["StatusMessage"] = "Maintenance task updated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
