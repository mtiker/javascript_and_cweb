using System.Globalization;
using App.BLL.Services;
using App.DAL.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public class ProfileController(
    AppDbContext dbContext,
    IUserContextService userContextService,
    App.BLL.Services.IAuthorizationService authorizationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var gymId = context.ActiveGymId.Value;
        var culture = CultureInfo.CurrentUICulture.Name;
        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId);
        if (currentMember == null)
        {
            return View(new ClientProfilePageViewModel
            {
                GymCode = context.ActiveGymCode,
                ActiveRole = context.ActiveRole,
                ProfileAvailable = false
            });
        }

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstAsync(entity => entity.Id == currentMember.Id);

        var memberships = await dbContext.Memberships
            .Where(entity => entity.GymId == gymId && entity.MemberId == member.Id)
            .OrderByDescending(entity => entity.StartDate)
            .Select(entity => new ClientMembershipSummaryViewModel
            {
                PackageName = entity.MembershipPackage!.Name.Translate(culture) ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status,
                PriceAtPurchase = entity.PriceAtPurchase,
                CurrencyCode = entity.CurrencyCode
            })
            .ToArrayAsync();

        var bookings = await dbContext.Bookings
            .Where(entity => entity.GymId == gymId && entity.MemberId == member.Id)
            .OrderByDescending(entity => entity.BookedAtUtc)
            .Select(entity => new ClientBookingSummaryViewModel
            {
                SessionName = entity.TrainingSession!.Name.Translate(culture) ?? string.Empty,
                StartAtUtc = entity.TrainingSession.StartAtUtc,
                Status = entity.Status,
                ChargedPrice = entity.ChargedPrice,
                CurrencyCode = entity.CurrencyCode
            })
            .ToArrayAsync();

        var payments = await dbContext.Payments
            .Where(entity => entity.GymId == gymId &&
                             ((entity.Membership != null && entity.Membership.MemberId == member.Id) ||
                              (entity.Booking != null && entity.Booking.MemberId == member.Id)))
            .OrderByDescending(entity => entity.PaidAtUtc)
            .Select(entity => new ClientPaymentSummaryViewModel
            {
                Amount = entity.Amount,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status,
                PaidAtUtc = entity.PaidAtUtc,
                Reference = entity.Reference
            })
            .ToArrayAsync();

        return View(new ClientProfilePageViewModel
        {
            GymCode = context.ActiveGymCode,
            ActiveRole = context.ActiveRole,
            ProfileAvailable = true,
            MemberName = $"{member.Person?.FirstName} {member.Person?.LastName}".Trim(),
            MemberCode = member.MemberCode,
            MemberStatus = member.Status,
            Memberships = memberships,
            Bookings = bookings,
            Payments = payments
        });
    }
}
