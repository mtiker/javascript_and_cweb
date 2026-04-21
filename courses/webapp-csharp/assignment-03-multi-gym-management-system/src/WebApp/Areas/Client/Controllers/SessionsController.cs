using System.Globalization;
using App.BLL.Contracts;
using App.DAL.EF;
using App.Domain;
using App.Domain.Common;
using App.Domain.Enums;
using App.DTO.v1.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class SessionsController(
    AppDbContext dbContext,
    IUserContextService userContextService,
    App.BLL.Contracts.IAuthorizationService authorizationService,
    ITrainingWorkflowService trainingWorkflowService,
    IMaintenanceWorkflowService maintenanceWorkflowService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new SessionsPageViewModel
        {
            GymCode = context.ActiveGymCode,
            Sessions = await trainingWorkflowService.GetSessionsAsync(context.ActiveGymCode),
            OpeningHours = await maintenanceWorkflowService.GetOpeningHoursAsync(context.ActiveGymCode),
            OpeningHourExceptions = await maintenanceWorkflowService.GetOpeningHourExceptionsAsync(context.ActiveGymCode)
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var session = await trainingWorkflowService.GetSessionAsync(context.ActiveGymCode, id);
        var culture = CultureInfo.CurrentUICulture.Name;
        var categoryName = await dbContext.TrainingCategories
            .Where(entity => entity.Id == session.CategoryId)
            .Select(entity => entity.Name.Translate(culture))
            .FirstOrDefaultAsync() ?? string.Empty;

        var trainerNames = await dbContext.WorkShifts
            .Where(entity => entity.TrainingSessionId == id && entity.ShiftType == ShiftType.Training)
            .Select(entity => $"{entity.Contract!.Staff!.Person!.FirstName} {entity.Contract.Staff.Person.LastName}".Trim())
            .Distinct()
            .ToArrayAsync();

        var currentMember = await authorizationService.GetCurrentMemberAsync(context.ActiveGymId.Value);
        var currentBooking = currentMember == null
            ? null
            : await dbContext.Bookings.FirstOrDefaultAsync(entity =>
                entity.TrainingSessionId == id &&
                entity.MemberId == currentMember.Id &&
                entity.Status != BookingStatus.Cancelled);

        return View(new SessionDetailPageViewModel
        {
            GymCode = context.ActiveGymCode,
            Session = session,
            CategoryName = categoryName,
            TrainerNames = trainerNames,
            OpeningHours = await maintenanceWorkflowService.GetOpeningHoursAsync(context.ActiveGymCode),
            OpeningHourExceptions = await maintenanceWorkflowService.GetOpeningHourExceptionsAsync(context.ActiveGymCode),
            CurrentMemberId = currentMember?.Id,
            CurrentBookingId = currentBooking?.Id,
            CurrentBookingStatus = currentBooking?.Status,
            CanManageRoster = await CanManageRosterAsync(context, id)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(Guid id, string? paymentReference)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var currentMember = await authorizationService.GetCurrentMemberAsync(context.ActiveGymId.Value);
        if (currentMember == null)
        {
            TempData["StatusMessage"] = "Only member accounts can book training sessions.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await trainingWorkflowService.CreateBookingAsync(context.ActiveGymCode, new BookingCreateRequest
            {
                TrainingSessionId = id,
                MemberId = currentMember.Id,
                PaymentReference = paymentReference
            });
            TempData["StatusMessage"] = "Booking confirmed.";
        }
        catch (Exception ex)
        {
            TempData["StatusMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(Guid id, Guid bookingId)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        await trainingWorkflowService.CancelBookingAsync(context.ActiveGymCode, bookingId);
        TempData["StatusMessage"] = "Booking cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Roster(Guid id)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!await CanManageRosterAsync(context, id))
        {
            return Forbid();
        }

        var session = await trainingWorkflowService.GetSessionAsync(context.ActiveGymCode, id);
        var bookings = await dbContext.Bookings
            .Where(entity => entity.TrainingSessionId == id)
            .OrderBy(entity => entity.Member!.Person!.LastName)
            .Select(entity => new TrainerRosterBookingViewModel
            {
                BookingId = entity.Id,
                MemberName = $"{entity.Member!.Person!.FirstName} {entity.Member.Person.LastName}".Trim(),
                Status = entity.Status,
                ChargedPrice = entity.ChargedPrice,
                PaymentRequired = entity.PaymentRequired
            })
            .ToArrayAsync();

        return View(new TrainerRosterPageViewModel
        {
            GymCode = context.ActiveGymCode,
            SessionId = id,
            SessionName = session.Name,
            StartAtUtc = session.StartAtUtc,
            Bookings = bookings
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttendance(Guid sessionId, Guid bookingId, BookingStatus status)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        await trainingWorkflowService.UpdateAttendanceAsync(context.ActiveGymCode, bookingId, new AttendanceUpdateRequest
        {
            Status = status
        });
        TempData["StatusMessage"] = "Attendance updated.";
        return RedirectToAction(nameof(Roster), new { id = sessionId });
    }

    private async Task<bool> CanManageRosterAsync(UserExecutionContext context, Guid sessionId)
    {
        if (!context.ActiveGymId.HasValue)
        {
            return false;
        }

        if (context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin))
        {
            return true;
        }

        if (!context.HasRole(RoleNames.Trainer))
        {
            return false;
        }

        var staff = await authorizationService.GetCurrentStaffAsync(context.ActiveGymId.Value);
        if (staff == null)
        {
            return false;
        }

        return await dbContext.WorkShifts.AnyAsync(shift =>
            shift.GymId == context.ActiveGymId.Value &&
            shift.TrainingSessionId == sessionId &&
            shift.Contract!.StaffId == staff.Id &&
            shift.ShiftType == ShiftType.Training);
    }
}
