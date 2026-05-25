using System.Globalization;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using SharedKernel;
using Base.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Shared.Contracts.Dtos.v1.TrainingSessions;
using Shared.Contracts.Mediator.Notifications;
using Shared.Contracts.ModuleApis;
using Modules.Training.Application.Mappers;
using Modules.Training.Application.Persistence;
using Modules.Training.Application.Pricing;

namespace Modules.Training.Application;

public class TrainingWorkflowService(
    ITrainingPersistenceContext persistenceContext,
    ITrainingCategoryRepository trainingCategoryRepository,
    ITrainingSessionRepository trainingSessionRepository,
    IBookingRepository bookingRepository,
    IGymsModuleApi gymsModuleApi,
    ITrainingModuleApi trainingModuleApi,
    IMembershipsModuleApi membershipsModuleApi,
    IAuthorizationService authorizationService,
    IUserContextService userContextService,
    IMembershipWorkflowService membershipWorkflowService,
    ISubscriptionTierLimitService subscriptionTierLimitService,
    ITrainingMapper trainingMapper,
    IBookingPricingService bookingPricingService,
    IMediator mediator,
    ILogger<TrainingWorkflowService> logger) : ITrainingWorkflowService
{
    public async Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);
        var categories = await trainingCategoryRepository.ListByGymAsync(gymId, cancellationToken);
        return trainingMapper.ToCategoryList(categories);
    }

    public async Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        ValidateCategoryRequest(request);

        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = ToLangStr(request.Name),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };

        await trainingCategoryRepository.AddAsync(category, cancellationToken);
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToCategory(category);
    }

    public async Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        ValidateCategoryRequest(request);

        var category = await trainingCategoryRepository.FindAsync(gymId, id, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");

        category.Name = ToLangStr(request.Name);
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await persistenceContext.SaveChangesAsync(cancellationToken);

        return trainingMapper.ToCategory(category);
    }

    public async Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var category = await trainingCategoryRepository.FindAsync(gymId, id, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");
        trainingCategoryRepository.Remove(category);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, TrainingSessionFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);

        var hasFilter = filter is not null && (filter.Status.HasValue || filter.CategoryId.HasValue || filter.TrainerStaffId.HasValue || filter.FromUtc.HasValue || filter.ToUtc.HasValue);
        var sessions = hasFilter
            ? await trainingSessionRepository.ListByGymFilteredAsync(gymId, filter!.Status, filter.CategoryId, filter.TrainerStaffId, filter.FromUtc, filter.ToUtc, cancellationToken)
            : await trainingSessionRepository.ListByGymAsync(gymId, cancellationToken);

        return sessions.Select(trainingMapper.ToSession).ToArray();
    }

    public async Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        return await MapSessionAsync(gymId, id, cancellationToken);
    }

    public async Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (request.EndAtUtc <= request.StartAtUtc)
        {
            throw new ValidationAppException("Session end time must be later than start time.");
        }

        var category = await trainingCategoryRepository.FindAsync(gymId, request.CategoryId, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");

        if (request.TrainerStaffId.HasValue)
        {
            if (await trainingModuleApi.GetStaffSummaryAsync(gymId, request.TrainerStaffId.Value, cancellationToken) is null)
            {
                throw new ValidationAppException("Trainer staff member was not found.");
            }
        }

        var session = sessionId.HasValue
            ? await trainingSessionRepository.FindAsync(gymId, sessionId.Value, cancellationToken)
            : null;

        if (sessionId.HasValue && session is null)
        {
            throw new NotFoundException("Training session was not found.");
        }

        if (session is null)
        {
            await subscriptionTierLimitService.EnsureCanCreateTrainingSessionAsync(gymId, cancellationToken);
            session = new TrainingSession { GymId = gymId };
            await trainingSessionRepository.AddAsync(session, cancellationToken);
        }

        session.CategoryId = category.Id;
        session.Name = ToLangStr(request.Name);
        session.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        session.StartAtUtc = request.StartAtUtc;
        session.EndAtUtc = request.EndAtUtc;
        session.Capacity = request.Capacity;
        session.BasePrice = request.BasePrice;
        session.CurrencyCode = request.CurrencyCode;
        session.Status = request.Status;
        session.TrainerStaffId = request.TrainerStaffId;

        await persistenceContext.SaveChangesAsync(cancellationToken);

        return await MapSessionAsync(gymId, session.Id, cancellationToken);
    }

    public async Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var session = await trainingSessionRepository.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");
        trainingSessionRepository.Remove(session);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrainingSessionResponse> UpdateSessionStatusAsync(string gymCode, Guid sessionId, TrainingSessionStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");
        session.Status = request.Status;
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToSession(session);
    }

    public async Task<TrainingSessionResponse> UpdateSessionTrainerAsync(string gymCode, Guid sessionId, TrainingSessionTrainerUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");

        if (request.TrainerStaffId.HasValue)
        {
            if (await trainingModuleApi.GetStaffSummaryAsync(gymId, request.TrainerStaffId.Value, cancellationToken) is null)
            {
                throw new ValidationAppException("Trainer staff member was not found.");
            }
        }

        session.TrainerStaffId = request.TrainerStaffId;
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return await MapSessionAsync(gymId, session.Id, cancellationToken);
    }

    public async Task<TrainingSessionResponse> RescheduleSessionAsync(string gymCode, Guid sessionId, TrainingSessionRescheduleRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (request.EndAtUtc <= request.StartAtUtc)
        {
            throw new ValidationAppException("Session end time must be later than start time.");
        }

        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");
        session.StartAtUtc = request.StartAtUtc;
        session.EndAtUtc = request.EndAtUtc;
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToSession(session);
    }

    public async Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, BookingFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);
        var current = userContextService.GetCurrent();

        IReadOnlyList<Booking> bookings;
        if (current.HasRole(RoleNames.Member))
        {
            var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
            bookings = member is null
                ? Array.Empty<Booking>()
                : await bookingRepository.ListForMemberAsync(gymId, member.Id, cancellationToken);
        }
        else if (current.HasRole(RoleNames.Trainer))
        {
            var staff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken);
            bookings = staff is null
                ? Array.Empty<Booking>()
                : await bookingRepository.ListForTrainerAsync(gymId, staff.Id, cancellationToken);
        }
        else if (filter is not null && (filter.Status.HasValue || filter.MemberId.HasValue || filter.TrainingSessionId.HasValue || filter.FromUtc.HasValue || filter.ToUtc.HasValue))
        {
            bookings = await bookingRepository.ListByGymFilteredAsync(gymId, filter.Status, filter.MemberId, filter.TrainingSessionId, filter.FromUtc, filter.ToUtc, cancellationToken);
        }
        else
        {
            bookings = await bookingRepository.ListByGymAsync(gymId, cancellationToken);
        }

        if (filter is not null && (current.HasRole(RoleNames.Member) || current.HasRole(RoleNames.Trainer)))
        {
            bookings = ApplyBookingFilterInMemory(bookings, filter);
        }

        return trainingMapper.ToBookingList(bookings);
    }

    private static IReadOnlyList<Booking> ApplyBookingFilterInMemory(IReadOnlyList<Booking> bookings, BookingFilter filter)
    {
        IEnumerable<Booking> q = bookings;
        if (filter.Status.HasValue) q = q.Where(b => b.Status == filter.Status.Value);
        if (filter.MemberId.HasValue) q = q.Where(b => b.MemberId == filter.MemberId.Value);
        if (filter.TrainingSessionId.HasValue) q = q.Where(b => b.TrainingSessionId == filter.TrainingSessionId.Value);
        if (filter.FromUtc.HasValue) q = q.Where(b => b.BookedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(b => b.BookedAtUtc <= filter.ToUtc.Value);
        return q.ToArray();
    }

    public async Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        var trainingSession = await trainingSessionRepository.FindAsync(gymId, request.TrainingSessionId, cancellationToken)
                              ?? throw new NotFoundException("Training session was not found.");

        var member = await membershipsModuleApi.GetMemberSummaryAsync(gymId, request.MemberId, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, member.Id, cancellationToken);

        var existingBooking = await bookingRepository.ExistsForMemberSessionAsync(gymId, member.Id, trainingSession.Id, cancellationToken);
        if (existingBooking)
        {
            throw new ValidationAppException("This member already has a booking for the selected session.");
        }

        if (trainingSession.Status != TrainingSessionStatus.Published)
        {
            throw new ValidationAppException("Bookings can be created only for published sessions.");
        }

        var bookedCount = await bookingRepository.CountActiveForSessionAsync(trainingSession.Id, cancellationToken);
        if (bookedCount >= trainingSession.Capacity)
        {
            throw new ValidationAppException("Training session capacity has been reached.");
        }

        var settings = await gymsModuleApi.GetSettingsAsync(gymId, cancellationToken)
            ?? throw new NotFoundException("Gym settings were not found.");

        var sessionSummary = new TrainingSessionSummary(
            trainingSession.Id,
            trainingSession.GymId,
            trainingSession.CategoryId,
            trainingSession.TrainerStaffId,
            trainingSession.Name.ToString(),
            trainingSession.StartAtUtc,
            trainingSession.EndAtUtc,
            trainingSession.Capacity,
            trainingSession.Status.ToString(),
            trainingSession.BasePrice,
            trainingSession.CurrencyCode);
        var chargedPrice = await bookingPricingService.CalculateBookingPriceAsync(gymId, member.Id, sessionSummary, cancellationToken);
        if (!settings.AllowNonMemberBookings && chargedPrice == trainingSession.BasePrice)
        {
            throw new ValidationAppException("This gym does not allow non-member bookings.");
        }

        var paymentRequired = chargedPrice > 0m;
        if (paymentRequired && string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            throw new ValidationAppException("Payment reference is required when payment is due.");
        }

        var booking = new Booking
        {
            GymId = gymId,
            TrainingSessionId = trainingSession.Id,
            TrainingSession = trainingSession,
            MemberId = member.Id,
            Status = BookingStatus.Booked,
            ChargedPrice = chargedPrice,
            CurrencyCode = trainingSession.CurrencyCode,
            PaymentRequired = paymentRequired
        };

        await bookingRepository.AddAsync(booking, cancellationToken);
        await persistenceContext.SaveChangesAsync(cancellationToken);

        if (paymentRequired)
        {
            await membershipWorkflowService.CreatePaymentAsync(gymCode, new()
            {
                BookingId = booking.Id,
                Amount = chargedPrice,
                CurrencyCode = trainingSession.CurrencyCode,
                Reference = request.PaymentReference!
            }, cancellationToken);
        }

        await PublishBookingConfirmedAsync(booking, cancellationToken);

        return trainingMapper.ToBooking(booking, member);
    }

    private async Task PublishBookingConfirmedAsync(Booking booking, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(
                new BookingConfirmedNotification(
                    booking.GymId,
                    booking.Id,
                    booking.MemberId,
                    booking.TrainingSessionId,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // A subscriber failure must not roll back a booking the caller already
            // sees as confirmed. The booking row exists; downstream consumers can
            // reconcile from the persisted state.
            logger.LogError(
                ex,
                "Failed to publish BookingConfirmedNotification for booking {BookingId} in gym {GymId}.",
                booking.Id,
                booking.GymId);
        }
    }

    public async Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);

        var booking = await bookingRepository.FindWithTrainingSessionAndMemberAsync(gymId, bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking was not found.");

        await authorizationService.EnsureTrainingAttendanceAccessAsync(booking.TrainingSession!, cancellationToken);

        booking.Status = request.Status;
        if (request.Status == BookingStatus.Cancelled)
        {
            booking.CancelledAtUtc = DateTime.UtcNow;
        }

        await persistenceContext.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToBooking(booking);
    }

    public async Task<BookingResponse> RescheduleBookingAsync(string gymCode, Guid bookingId, BookingRescheduleRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        var booking = await bookingRepository.FindWithTrainingSessionAndMemberAsync(gymId, bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking was not found.");

        await authorizationService.EnsureBookingAccessAsync(booking, cancellationToken);

        if (booking.Status != BookingStatus.Booked)
        {
            throw new ValidationAppException("Only active bookings can be rescheduled.");
        }

        if (booking.TrainingSessionId == request.TrainingSessionId)
        {
            return trainingMapper.ToBooking(booking);
        }

        var newSession = await trainingSessionRepository.FindAsync(gymId, request.TrainingSessionId, cancellationToken)
                         ?? throw new NotFoundException("Training session was not found.");

        if (newSession.Status != TrainingSessionStatus.Published)
        {
            throw new ValidationAppException("Bookings can only be moved to published sessions.");
        }

        var alreadyBooked = await bookingRepository.ExistsForMemberSessionAsync(gymId, booking.MemberId, newSession.Id, cancellationToken);
        if (alreadyBooked)
        {
            throw new ValidationAppException("This member already has a booking for the selected session.");
        }

        var bookedCount = await bookingRepository.CountActiveForSessionAsync(newSession.Id, cancellationToken);
        if (bookedCount >= newSession.Capacity)
        {
            throw new ValidationAppException("Training session capacity has been reached.");
        }

        booking.TrainingSessionId = newSession.Id;
        booking.TrainingSession = newSession;
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToBooking(booking);
    }

    public async Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var booking = await bookingRepository.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Booking was not found.");
        await authorizationService.EnsureBookingAccessAsync(booking, cancellationToken);
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TrainingSessionResponse> MapSessionAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await trainingSessionRepository.FindAsync(gymId, sessionId, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");
        return trainingMapper.ToSession(session);
    }

    private static void ValidateCategoryRequest(TrainingCategoryUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationAppException("Training category name is required.");
        }
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }
}
