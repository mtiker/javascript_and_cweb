using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Payments;

namespace App.BLL.Services;

public class PaymentService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper) : IPaymentService
{
    public async Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        IReadOnlyList<Payment> payments;

        if (member != null)
        {
            var membershipIds = await unitOfWork.Memberships.ListIdsForMemberAsync(gymId, member.Id, cancellationToken);
            var bookings = await unitOfWork.Bookings.ListForMemberAsync(gymId, member.Id, cancellationToken);
            var bookingIds = bookings.Select(booking => booking.Id).ToArray();
            payments = await unitOfWork.Payments.ListForMembershipOrBookingIdsAsync(gymId, membershipIds, bookingIds, cancellationToken);
        }
        else
        {
            payments = await unitOfWork.Payments.ListByGymAsync(gymId, cancellationToken);
        }

        return mapper.ToPaymentResponses(payments);
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
            var membership = await unitOfWork.Memberships.FindAsync(gymId, request.MembershipId.Value, cancellationToken)
                             ?? throw new NotFoundException("Membership was not found.");

            await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        }

        if (request.BookingId.HasValue)
        {
            var booking = await unitOfWork.Bookings.FindAsync(gymId, request.BookingId.Value, cancellationToken)
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

        await unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.ToPaymentResponse(payment);
    }
}
