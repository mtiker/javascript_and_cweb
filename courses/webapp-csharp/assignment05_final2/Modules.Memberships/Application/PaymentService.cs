using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using Modules.Memberships.Application.Mappers;
using SharedKernel;
using App.Domain.Entities;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Payments;
using Shared.Contracts.ModuleApis;

namespace Modules.Memberships.Application;

public class PaymentService(
    IAppDbContext dbContext,
    IMembershipRepository membershipRepository,
    IPaymentRepository paymentRepository,
    ITrainingModuleApi trainingModuleApi,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper) : IPaymentService
{
    public async Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, PaymentFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        IReadOnlyList<Payment> payments;
        var hasFilter = filter is not null && (filter.Status.HasValue || filter.MembershipId.HasValue || filter.BookingId.HasValue || filter.FromUtc.HasValue || filter.ToUtc.HasValue);

        if (member != null)
        {
            var membershipIds = await membershipRepository.ListIdsForMemberAsync(gymId, member.Id, cancellationToken);
            var bookingIds = await trainingModuleApi.ListBookingIdsForMemberAsync(gymId, member.Id, cancellationToken);
            payments = await paymentRepository.ListForMembershipOrBookingIdsAsync(gymId, membershipIds, bookingIds, cancellationToken);
            if (hasFilter)
            {
                payments = ApplyInMemory(payments, filter!);
            }
        }
        else if (hasFilter)
        {
            payments = await paymentRepository.ListByGymFilteredAsync(gymId, filter!.Status, filter.MembershipId, filter.BookingId, filter.FromUtc, filter.ToUtc, cancellationToken);
        }
        else
        {
            payments = await paymentRepository.ListByGymAsync(gymId, cancellationToken);
        }

        return mapper.ToPaymentResponses(payments);
    }

    public async Task<PaymentResponse> RefundPaymentAsync(string gymCode, Guid paymentId, PaymentRefundRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var payment = await paymentRepository.FindAsync(gymId, paymentId, cancellationToken)
                      ?? throw new NotFoundException("Payment was not found.");

        if (payment.Status == PaymentStatus.Refunded)
        {
            throw new ValidationAppException("Payment is already refunded.");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new ValidationAppException("Only completed payments can be refunded.");
        }

        var refundAmount = request.Amount ?? payment.Amount;
        if (refundAmount <= 0 || refundAmount > payment.Amount)
        {
            throw new ValidationAppException("Refund amount must be greater than zero and not exceed the original amount.");
        }

        payment.Status = PaymentStatus.Refunded;

        var refundReference = string.IsNullOrWhiteSpace(request.Reason)
            ? $"REFUND-{payment.Id}"
            : $"REFUND-{payment.Id}-{request.Reason.Trim()}";

        await paymentRepository.AddAsync(new Payment
        {
            GymId = gymId,
            MembershipId = payment.MembershipId,
            BookingId = payment.BookingId,
            Amount = -refundAmount,
            CurrencyCode = payment.CurrencyCode,
            Status = PaymentStatus.Refunded,
            Reference = refundReference
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return mapper.ToPaymentResponse(payment);
    }

    private static IReadOnlyList<Payment> ApplyInMemory(IReadOnlyList<Payment> payments, PaymentFilter filter)
    {
        IEnumerable<Payment> q = payments;
        if (filter.Status.HasValue) q = q.Where(p => p.Status == filter.Status.Value);
        if (filter.MembershipId.HasValue) q = q.Where(p => p.MembershipId == filter.MembershipId.Value);
        if (filter.BookingId.HasValue) q = q.Where(p => p.BookingId == filter.BookingId.Value);
        if (filter.FromUtc.HasValue) q = q.Where(p => p.PaidAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(p => p.PaidAtUtc <= filter.ToUtc.Value);
        return q.ToArray();
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
            var membership = await membershipRepository.FindAsync(gymId, request.MembershipId.Value, cancellationToken)
                             ?? throw new NotFoundException("Membership was not found.");

            await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        }

        if (request.BookingId.HasValue)
        {
            var booking = await trainingModuleApi.GetBookingSummaryAsync(gymId, request.BookingId.Value, cancellationToken)
                          ?? throw new NotFoundException("Booking was not found.");

            await authorizationService.EnsureBookingAccessAsync(
                booking.GymId,
                booking.MemberId,
                booking.TrainingSessionId,
                cancellationToken);
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

        await paymentRepository.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.ToPaymentResponse(payment);
    }
}
