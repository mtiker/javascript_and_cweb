using App.BLL.Contracts;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class DashboardController(AppDbContext dbContext, IUserContextService userContextService, App.BLL.Contracts.IAuthorizationService authorizationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        var gymId = context.ActiveGymId.Value;
        var sessions = await dbContext.TrainingSessions
            .Where(entity => entity.GymId == gymId && entity.StartAtUtc >= DateTime.UtcNow)
            .OrderBy(entity => entity.StartAtUtc)
            .Take(5)
            .Select(entity => new TrainingSessionResponse
            {
                Id = entity.Id,
                CategoryId = entity.CategoryId,
                Name = entity.Name.Translate() ?? string.Empty,
                Description = entity.Description == null ? null : entity.Description.Translate(),
                StartAtUtc = entity.StartAtUtc,
                EndAtUtc = entity.EndAtUtc,
                Capacity = entity.Capacity,
                BasePrice = entity.BasePrice,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status
            })
            .ToArrayAsync();

        var viewModel = new ClientDashboardViewModel
        {
            ActiveGymCode = context.ActiveGymCode,
            ActiveRole = context.ActiveRole,
            UpcomingSessions = sessions
        };

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId);
        if (currentMember != null)
        {
            viewModel.MyBookings = await dbContext.Bookings
                .Where(entity => entity.GymId == gymId && entity.MemberId == currentMember.Id)
                .OrderByDescending(entity => entity.BookedAtUtc)
                .Take(5)
                .Select(entity => new BookingResponse
                {
                    Id = entity.Id,
                    TrainingSessionId = entity.TrainingSessionId,
                    MemberId = entity.MemberId,
                    Status = entity.Status,
                    ChargedPrice = entity.ChargedPrice,
                    PaymentRequired = entity.PaymentRequired
                })
                .ToArrayAsync();
        }

        var currentStaff = await authorizationService.GetCurrentStaffAsync(gymId);
        if (currentStaff != null)
        {
            viewModel.AssignedTasks = await dbContext.MaintenanceTasks
                .Where(entity => entity.GymId == gymId && entity.AssignedStaffId == currentStaff.Id)
                .OrderBy(entity => entity.DueAtUtc)
                .Take(5)
                .Select(entity => new MaintenanceTaskResponse
                {
                    Id = entity.Id,
                    EquipmentId = entity.EquipmentId,
                    AssignedStaffId = entity.AssignedStaffId,
                    CreatedByStaffId = entity.CreatedByStaffId,
                    TaskType = entity.TaskType,
                    Priority = entity.Priority,
                    Status = entity.Status,
                    DueAtUtc = entity.DueAtUtc,
                    StartedAtUtc = entity.StartedAtUtc,
                    CompletedAtUtc = entity.CompletedAtUtc,
                    Notes = entity.Notes
                })
                .ToArrayAsync();
        }

        return View(viewModel);
    }
}
