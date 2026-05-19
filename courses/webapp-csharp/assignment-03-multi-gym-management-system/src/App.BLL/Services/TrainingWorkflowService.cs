using System.Globalization;
using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;

namespace App.BLL.Services;

public class TrainingWorkflowService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IUserContextService userContextService,
    IMembershipWorkflowService membershipWorkflowService,
    ISubscriptionTierLimitService subscriptionTierLimitService,
    ITrainingMapper trainingMapper) : ITrainingWorkflowService
{
    public async Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);
        var categories = await unitOfWork.TrainingCategories.ListByGymAsync(gymId, cancellationToken);
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

        await unitOfWork.TrainingCategories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToCategory(category);
    }

    public async Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        ValidateCategoryRequest(request);

        var category = await unitOfWork.TrainingCategories.FindAsync(gymId, id, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");

        category.Name = ToLangStr(request.Name);
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return trainingMapper.ToCategory(category);
    }

    public async Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var category = await unitOfWork.TrainingCategories.FindAsync(gymId, id, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");
        unitOfWork.TrainingCategories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var sessions = await unitOfWork.TrainingSessions.ListByGymAsync(gymId, cancellationToken);
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

        var category = await unitOfWork.TrainingCategories.FindAsync(gymId, request.CategoryId, cancellationToken)
                       ?? throw new NotFoundException("Training category was not found.");

        if (request.TrainerStaffId.HasValue)
        {
            var trainerStaff = await unitOfWork.Repository<Staff>().ListAsync(
                staff => staff.GymId == gymId && staff.Id == request.TrainerStaffId.Value,
                cancellationToken);

            if (trainerStaff.Count == 0)
            {
                throw new ValidationAppException("Trainer staff member was not found.");
            }
        }

        var session = sessionId.HasValue
            ? await unitOfWork.TrainingSessions.FindAsync(gymId, sessionId.Value, cancellationToken)
            : null;

        if (sessionId.HasValue && session is null)
        {
            throw new NotFoundException("Training session was not found.");
        }

        if (session is null)
        {
            await subscriptionTierLimitService.EnsureCanCreateTrainingSessionAsync(gymId, cancellationToken);
            session = new TrainingSession { GymId = gymId };
            await unitOfWork.TrainingSessions.AddAsync(session, cancellationToken);
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapSessionAsync(gymId, session.Id, cancellationToken);
    }

    public async Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var session = await unitOfWork.TrainingSessions.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Training session was not found.");
        unitOfWork.TrainingSessions.Remove(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);
        var current = userContextService.GetCurrent();

        IReadOnlyList<Booking> bookings;
        if (current.HasRole(RoleNames.Member))
        {
            var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
            bookings = member is null
                ? Array.Empty<Booking>()
                : await unitOfWork.Bookings.ListForMemberAsync(gymId, member.Id, cancellationToken);
        }
        else if (current.HasRole(RoleNames.Trainer))
        {
            var staff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken);
            bookings = staff is null
                ? Array.Empty<Booking>()
                : await unitOfWork.Bookings.ListForTrainerAsync(gymId, staff.Id, cancellationToken);
        }
        else
        {
            bookings = await unitOfWork.Bookings.ListByGymAsync(gymId, cancellationToken);
        }

        return trainingMapper.ToBookingList(bookings);
    }

    public async Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        var trainingSession = await unitOfWork.TrainingSessions.FindAsync(gymId, request.TrainingSessionId, cancellationToken)
                              ?? throw new NotFoundException("Training session was not found.");

        var member = await unitOfWork.Members.FindWithPersonAsync(gymId, request.MemberId, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, member.Id, cancellationToken);

        var existingBooking = await unitOfWork.Bookings.ExistsForMemberSessionAsync(gymId, member.Id, trainingSession.Id, cancellationToken);
        if (existingBooking)
        {
            throw new ValidationAppException("This member already has a booking for the selected session.");
        }

        if (trainingSession.Status != TrainingSessionStatus.Published)
        {
            throw new ValidationAppException("Bookings can be created only for published sessions.");
        }

        var bookedCount = await unitOfWork.Bookings.CountActiveForSessionAsync(trainingSession.Id, cancellationToken);
        if (bookedCount >= trainingSession.Capacity)
        {
            throw new ValidationAppException("Training session capacity has been reached.");
        }

        var settings = (await unitOfWork.Repository<GymSettings>().ListAsync(entity => entity.GymId == gymId, cancellationToken))
            .FirstOrDefault()
            ?? throw new NotFoundException("Gym settings were not found.");

        var chargedPrice = await membershipWorkflowService.CalculateBookingPriceAsync(gymId, member.Id, trainingSession, cancellationToken);
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
            Member = member,
            Status = BookingStatus.Booked,
            ChargedPrice = chargedPrice,
            CurrencyCode = trainingSession.CurrencyCode,
            PaymentRequired = paymentRequired
        };

        await unitOfWork.Bookings.AddAsync(booking, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (paymentRequired)
        {
            await unitOfWork.Repository<Payment>().AddAsync(new Payment
            {
                GymId = gymId,
                BookingId = booking.Id,
                Amount = chargedPrice,
                CurrencyCode = trainingSession.CurrencyCode,
                Status = PaymentStatus.Completed,
                Reference = request.PaymentReference
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return trainingMapper.ToBooking(booking);
    }

    public async Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);

        var booking = await unitOfWork.Bookings.FindWithTrainingSessionAndMemberAsync(gymId, bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking was not found.");

        await authorizationService.EnsureTrainingAttendanceAccessAsync(booking.TrainingSession!, cancellationToken);

        booking.Status = request.Status;
        if (request.Status == BookingStatus.Cancelled)
        {
            booking.CancelledAtUtc = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return trainingMapper.ToBooking(booking);
    }

    public async Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var booking = await unitOfWork.Bookings.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Booking was not found.");
        await authorizationService.EnsureBookingAccessAsync(booking, cancellationToken);
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TrainingSessionResponse> MapSessionAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await unitOfWork.TrainingSessions.FindAsync(gymId, sessionId, cancellationToken)
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
