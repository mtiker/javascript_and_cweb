using Shared.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Client.Services;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class SessionsController(IClientSessionsPageService sessionsPageService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.BuildIndexAsync(cancellationToken);
        if (result.Status == ClientSessionsPageStatus.MissingGymContext)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(result.ViewModel);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.BuildDetailsAsync(id, cancellationToken);
        if (result.Status == ClientSessionsPageStatus.MissingGymContext)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(result.ViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(Guid id, string? paymentReference, CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.BookAsync(id, paymentReference, cancellationToken);
        if (result.Status == ClientSessionCommandStatus.MissingGymContext)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(Guid id, Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.CancelBookingAsync(bookingId, cancellationToken);
        if (result.Status == ClientSessionCommandStatus.MissingGymContext)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Roster(Guid id, CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.BuildRosterAsync(id, cancellationToken);
        return result.Status switch
        {
            ClientSessionsPageStatus.MissingGymContext => RedirectToAction("Index", "Dashboard"),
            ClientSessionsPageStatus.Forbidden => Forbid(),
            _ => View(result.ViewModel)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttendance(
        Guid sessionId,
        Guid bookingId,
        BookingStatus status,
        CancellationToken cancellationToken)
    {
        var result = await sessionsPageService.UpdateAttendanceAsync(bookingId, status, cancellationToken);
        if (result.Status == ClientSessionCommandStatus.MissingGymContext)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Roster), new { id = sessionId });
    }
}
