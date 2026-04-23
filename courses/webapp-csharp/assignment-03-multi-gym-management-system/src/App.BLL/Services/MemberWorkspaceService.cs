using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Finance;
using App.DTO.v1.MemberWorkspace;
using App.DTO.v1.Members;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class MemberWorkspaceService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMemberWorkspaceService
{
    public async Task<MemberWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                            ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");

        return await BuildWorkspaceAsync(gymId, currentMember.Id, cancellationToken);
    }

    public async Task<MemberWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member,
            RoleNames.Trainer);

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId, cancellationToken);

        return await BuildWorkspaceAsync(gymId, memberId, cancellationToken);
    }

    private async Task<MemberWorkspaceResponse> BuildWorkspaceAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken)
    {
        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == memberId, cancellationToken)
            ?? throw new NotFoundException("Member was not found.");

        var memberships = await dbContext.Memberships
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .OrderByDescending(entity => entity.StartDate)
            .ToListAsync(cancellationToken);

        var membershipIds = memberships.Select(entity => entity.Id).ToArray();

        var bookings = await dbContext.Bookings
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .Include(entity => entity.TrainingSession)
            .OrderByDescending(entity => entity.BookedAtUtc)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(entity => entity.Id).ToArray();

        var payments = await dbContext.Payments
            .Where(entity => entity.GymId == gymId)
            .Where(entity =>
                (entity.MembershipId.HasValue && membershipIds.Contains(entity.MembershipId.Value)) ||
                (entity.BookingId.HasValue && bookingIds.Contains(entity.BookingId.Value)))
            .OrderByDescending(entity => entity.PaidAtUtc)
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Invoices
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .OrderByDescending(entity => entity.DueAtUtc)
            .ToListAsync(cancellationToken);

        var outstandingActions = BuildOutstandingActions(memberships, payments, invoices, bookings);

        return new MemberWorkspaceResponse
        {
            Profile = new MemberDetailResponse
            {
                Id = member.Id,
                MemberCode = member.MemberCode,
                FirstName = member.Person?.FirstName ?? string.Empty,
                LastName = member.Person?.LastName ?? string.Empty,
                FullName = $"{member.Person?.FirstName} {member.Person?.LastName}".Trim(),
                PersonalCode = member.Person?.PersonalCode,
                DateOfBirth = member.Person?.DateOfBirth,
                Status = member.Status
            },
            Memberships = memberships.Select(entity => new MembershipResponse
            {
                Id = entity.Id,
                MemberId = entity.MemberId,
                MembershipPackageId = entity.MembershipPackageId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PriceAtPurchase = entity.PriceAtPurchase,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status
            }).ToArray(),
            Payments = payments.Select(entity => new PaymentResponse
            {
                Id = entity.Id,
                Amount = entity.Amount,
                CurrencyCode = entity.CurrencyCode,
                PaidAtUtc = entity.PaidAtUtc,
                Status = entity.Status,
                Reference = entity.Reference,
                MembershipId = entity.MembershipId,
                BookingId = entity.BookingId
            }).ToArray(),
            Bookings = bookings.Select(entity => new MemberWorkspaceBookingResponse
            {
                BookingId = entity.Id,
                TrainingSessionId = entity.TrainingSessionId,
                TrainingSessionName = entity.TrainingSession?.Name.Translate(System.Globalization.CultureInfo.CurrentUICulture.Name) ?? "Session",
                StartAtUtc = entity.TrainingSession?.StartAtUtc ?? DateTime.MinValue,
                EndAtUtc = entity.TrainingSession?.EndAtUtc ?? DateTime.MinValue,
                Status = entity.Status,
                ChargedPrice = entity.ChargedPrice,
                CurrencyCode = entity.CurrencyCode,
                PaymentRequired = entity.PaymentRequired
            }).ToArray(),
            Invoices = invoices.Select(ToInvoiceResponse).ToArray(),
            AttendedSessionCount = bookings.Count(entity => entity.Status == BookingStatus.Attended),
            UpcomingBookingCount = bookings.Count(entity => entity.Status == BookingStatus.Booked && entity.TrainingSession?.StartAtUtc >= DateTime.UtcNow),
            OutstandingBalance = invoices.Sum(entity => entity.OutstandingAmount),
            OutstandingActions = outstandingActions
        };
    }

    private static IReadOnlyCollection<MemberOutstandingActionResponse> BuildOutstandingActions(
        IReadOnlyCollection<Membership> memberships,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Booking> bookings)
    {
        var actions = new List<MemberOutstandingActionResponse>();

        var overdueInvoices = invoices.Count(entity => entity.OutstandingAmount > 0 && entity.DueAtUtc < DateTime.UtcNow);
        if (overdueInvoices > 0)
        {
            actions.Add(new MemberOutstandingActionResponse
            {
                Code = "overdue-invoices",
                Title = "Overdue invoices",
                Detail = $"{overdueInvoices} invoice(s) are overdue and require payment."
            });
        }

        var pendingPayments = payments.Count(entity => entity.Status == PaymentStatus.Pending);
        if (pendingPayments > 0)
        {
            actions.Add(new MemberOutstandingActionResponse
            {
                Code = "pending-payments",
                Title = "Pending payments",
                Detail = $"{pendingPayments} payment record(s) are still pending confirmation."
            });
        }

        var soonExpiringMembership = memberships
            .Where(entity => entity.Status is MembershipStatus.Active or MembershipStatus.Renewed)
            .OrderBy(entity => entity.EndDate)
            .FirstOrDefault(entity => entity.EndDate <= DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)));

        if (soonExpiringMembership != null)
        {
            actions.Add(new MemberOutstandingActionResponse
            {
                Code = "membership-expiring",
                Title = "Membership ending soon",
                Detail = $"Current membership ends on {soonExpiringMembership.EndDate:yyyy-MM-dd}."
            });
        }

        var noShowCount = bookings.Count(entity => entity.Status == BookingStatus.NoShow);
        if (noShowCount > 0)
        {
            actions.Add(new MemberOutstandingActionResponse
            {
                Code = "no-show-follow-up",
                Title = "Attendance follow-up",
                Detail = $"{noShowCount} booking(s) are marked as no-show. Contact staff if this is incorrect."
            });
        }

        return actions;
    }

    private static InvoiceResponse ToInvoiceResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            MemberId = invoice.MemberId,
            MemberName = $"{invoice.Member?.Person?.FirstName} {invoice.Member?.Person?.LastName}".Trim(),
            InvoiceNumber = invoice.InvoiceNumber,
            IssuedAtUtc = invoice.IssuedAtUtc,
            DueAtUtc = invoice.DueAtUtc,
            CurrencyCode = invoice.CurrencyCode,
            SubtotalAmount = invoice.SubtotalAmount,
            CreditAmount = invoice.CreditAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            IsOverdue = invoice.OutstandingAmount > 0 && invoice.DueAtUtc < DateTime.UtcNow,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Lines = invoice.Lines
                .OrderBy(entity => entity.CreatedAtUtc)
                .Select(entity => new InvoiceLineResponse
                {
                    Id = entity.Id,
                    Description = entity.Description,
                    Quantity = entity.Quantity,
                    UnitPrice = entity.UnitPrice,
                    LineTotal = entity.LineTotal,
                    IsCredit = entity.IsCredit,
                    Notes = entity.Notes
                })
                .ToArray(),
            Payments = invoice.Payments
                .OrderByDescending(entity => entity.AppliedAtUtc)
                .Select(entity => new InvoicePaymentResponse
                {
                    Id = entity.Id,
                    Amount = entity.Amount,
                    IsRefund = entity.IsRefund,
                    AppliedAtUtc = entity.AppliedAtUtc,
                    Reference = entity.Reference,
                    Notes = entity.Notes
                })
                .ToArray()
        };
    }
}
