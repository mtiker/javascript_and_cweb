using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Payments;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class PaymentService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IPaymentService
{
    public async Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var query = dbContext.Payments.Where(entity => entity.GymId == gymId);
        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);

        if (member != null)
        {
            var membershipIds = await dbContext.Memberships
                .Where(entity => entity.MemberId == member.Id)
                .Select(entity => entity.Id)
                .ToListAsync(cancellationToken);

            var bookingIds = await dbContext.Bookings
                .Where(entity => entity.MemberId == member.Id)
                .Select(entity => entity.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(entity =>
                (entity.MembershipId.HasValue && membershipIds.Contains(entity.MembershipId.Value)) ||
                (entity.BookingId.HasValue && bookingIds.Contains(entity.BookingId.Value)));
        }

        return await query
            .OrderByDescending(entity => entity.PaidAtUtc)
            .Select(entity => MembershipWorkflowMapping.ToPaymentResponse(entity))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        if (!request.MembershipId.HasValue && !request.BookingId.HasValue)
        {
            throw new ValidationAppException("Payment must be linked to a membership or a booking.");
        }

        if (request.MembershipId.HasValue)
        {
            var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.Id == request.MembershipId.Value, cancellationToken)
                             ?? throw new NotFoundException("Membership was not found.");

            await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        }

        if (request.BookingId.HasValue)
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(entity => entity.Id == request.BookingId.Value, cancellationToken)
                          ?? throw new NotFoundException("Booking was not found.");

            await authorizationService.EnsureBookingAccessAsync(booking, cancellationToken);
        }

        var payment = new Payment
        {
            GymId = gymId,
            MembershipId = request.MembershipId,
            BookingId = request.BookingId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Reference = request.Reference,
            Status = PaymentStatus.Completed
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MembershipWorkflowMapping.ToPaymentResponse(payment);
    }
}
