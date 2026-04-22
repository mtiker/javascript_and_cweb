using System.Globalization;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.WorkShifts;

namespace App.BLL.Services;

public class TrainingWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService,
    IUserContextService userContextService,
    IMembershipWorkflowService membershipWorkflowService) : ITrainingWorkflowService
{
    public async Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);

        return await dbContext.TrainingCategories
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ValidFrom)
            .Select(entity => new TrainingCategoryResponse
            {
                Id = entity.Id,
                Name = Translate(entity.Name) ?? string.Empty,
                Description = Translate(entity.Description)
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = ToLangStr(request.Name),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };

        dbContext.TrainingCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var category = await dbContext.TrainingCategories.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new NotFoundException("Training category was not found.");

        category.Name = ToLangStr(request.Name);
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToCategoryResponse(category);
    }

    public async Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var category = await dbContext.TrainingCategories.FirstOrDefaultAsync(entity => entity.Id == id)
                       ?? throw new NotFoundException("Training category was not found.");
        dbContext.TrainingCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var sessions = await dbContext.TrainingSessions
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.StartAtUtc)
            .Select(entity => new TrainingSessionResponse
            {
                Id = entity.Id,
                CategoryId = entity.CategoryId,
                Name = Translate(entity.Name) ?? string.Empty,
                Description = Translate(entity.Description),
                StartAtUtc = entity.StartAtUtc,
                EndAtUtc = entity.EndAtUtc,
                Capacity = entity.Capacity,
                BasePrice = entity.BasePrice,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status
            })
            .ToArrayAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.TrainerContractIds = await dbContext.WorkShifts
                .Where(entity => entity.TrainingSessionId == session.Id && entity.ShiftType == ShiftType.Training)
                .Select(entity => entity.ContractId)
                .ToListAsync(cancellationToken);
        }

        return sessions;
    }

    public async Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        return await ProjectSessionAsync(id, cancellationToken);
    }

    public async Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (request.EndAtUtc <= request.StartAtUtc)
        {
            throw new ValidationAppException("Session end time must be later than start time.");
        }

        var category = await dbContext.TrainingCategories.FirstOrDefaultAsync(entity =>
                           entity.GymId == gymId && entity.Id == request.CategoryId)
                       ?? throw new NotFoundException("Training category was not found.");

        var trainerContracts = await dbContext.EmploymentContracts
            .Where(entity => entity.GymId == gymId && request.TrainerContractIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        if (trainerContracts.Count != request.TrainerContractIds.Count)
        {
            throw new ValidationAppException("One or more trainer contracts were not found.");
        }

        var session = sessionId.HasValue
            ? await dbContext.TrainingSessions.FirstOrDefaultAsync(entity => entity.Id == sessionId.Value)
            : null;

        if (session == null)
        {
            session = new TrainingSession { GymId = gymId };
            dbContext.TrainingSessions.Add(session);
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

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingTrainerShifts = await dbContext.WorkShifts
            .Where(entity => entity.GymId == gymId && entity.TrainingSessionId == session.Id && entity.ShiftType == ShiftType.Training)
            .ToListAsync(cancellationToken);

        dbContext.WorkShifts.RemoveRange(existingTrainerShifts);

        foreach (var contract in trainerContracts)
        {
            dbContext.WorkShifts.Add(new WorkShift
            {
                GymId = gymId,
                ContractId = contract.Id,
                TrainingSessionId = session.Id,
                ShiftType = ShiftType.Training,
                StartAtUtc = request.StartAtUtc.AddMinutes(-15),
                EndAtUtc = request.EndAtUtc.AddMinutes(15),
                Comment = "Assigned trainer"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectSessionAsync(session.Id, cancellationToken);
    }

    public async Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var session = await dbContext.TrainingSessions.FirstOrDefaultAsync(entity => entity.Id == id)
                      ?? throw new NotFoundException("Training session was not found.");
        dbContext.TrainingSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkShiftResponse>> GetWorkShiftsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer, RoleNames.Caretaker);
        var current = userContextService.GetCurrent();
        var query = dbContext.WorkShifts.Where(entity => entity.GymId == gymId);

        if (current.HasRole(RoleNames.Trainer) || current.HasRole(RoleNames.Caretaker))
        {
            var staff = await authorizationService.GetCurrentStaffAsync(gymId);
            if (staff != null)
            {
                query = query.Where(entity => entity.Contract!.StaffId == staff.Id);
            }
        }

        return await query
            .OrderBy(entity => entity.StartAtUtc)
            .Select(entity => new WorkShiftResponse
            {
                Id = entity.Id,
                ContractId = entity.ContractId,
                StartAtUtc = entity.StartAtUtc,
                EndAtUtc = entity.EndAtUtc,
                ShiftType = entity.ShiftType,
                TrainingSessionId = entity.TrainingSessionId,
                Comment = entity.Comment
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<WorkShiftResponse> CreateWorkShiftAsync(string gymCode, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var shift = new WorkShift
        {
            GymId = gymId,
            ContractId = request.ContractId,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            ShiftType = request.ShiftType,
            TrainingSessionId = request.TrainingSessionId,
            Comment = request.Comment?.Trim()
        };

        dbContext.WorkShifts.Add(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToShiftResponse(shift);
    }

    public async Task<WorkShiftResponse> UpdateWorkShiftAsync(string gymCode, Guid id, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var shift = await dbContext.WorkShifts.FirstOrDefaultAsync(entity => entity.Id == id)
                    ?? throw new NotFoundException("Work shift was not found.");

        shift.ContractId = request.ContractId;
        shift.StartAtUtc = request.StartAtUtc;
        shift.EndAtUtc = request.EndAtUtc;
        shift.ShiftType = request.ShiftType;
        shift.TrainingSessionId = request.TrainingSessionId;
        shift.Comment = request.Comment?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToShiftResponse(shift);
    }

    public async Task DeleteWorkShiftAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var shift = await dbContext.WorkShifts.FirstOrDefaultAsync(entity => entity.Id == id)
                    ?? throw new NotFoundException("Work shift was not found.");
        dbContext.WorkShifts.Remove(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer);
        var current = userContextService.GetCurrent();
        var query = dbContext.Bookings.Where(entity => entity.GymId == gymId);

        if (current.HasRole(RoleNames.Member))
        {
            var member = await authorizationService.GetCurrentMemberAsync(gymId);
            if (member != null)
            {
                query = query.Where(entity => entity.MemberId == member.Id);
            }
        }
        else if (current.HasRole(RoleNames.Trainer))
        {
            var staff = await authorizationService.GetCurrentStaffAsync(gymId);
            if (staff != null)
            {
                var sessionIds = await dbContext.WorkShifts
                    .Where(entity => entity.GymId == gymId && entity.Contract!.StaffId == staff.Id && entity.TrainingSessionId.HasValue)
                    .Select(entity => entity.TrainingSessionId!.Value)
                    .ToListAsync(cancellationToken);
                query = query.Where(entity => sessionIds.Contains(entity.TrainingSessionId));
            }
        }

        return await query
            .OrderByDescending(entity => entity.BookedAtUtc)
            .Select(entity => new BookingResponse
            {
                Id = entity.Id,
                TrainingSessionId = entity.TrainingSessionId,
                TrainingSessionName = Translate(entity.TrainingSession!.Name) ?? string.Empty,
                MemberId = entity.MemberId,
                MemberName = $"{entity.Member!.Person!.FirstName} {entity.Member.Person.LastName}".Trim(),
                MemberCode = entity.Member.MemberCode,
                Status = entity.Status,
                ChargedPrice = entity.ChargedPrice,
                PaymentRequired = entity.PaymentRequired
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        var trainingSession = await dbContext.TrainingSessions.FirstOrDefaultAsync(entity =>
                                  entity.GymId == gymId && entity.Id == request.TrainingSessionId)
                              ?? throw new NotFoundException("Training session was not found.");

        var member = await dbContext.Members
                     .Include(entity => entity.Person)
                     .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == request.MemberId)
                     ?? throw new NotFoundException("Member was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, member.Id);

        var existingBooking = await dbContext.Bookings.FirstOrDefaultAsync(entity =>
            entity.GymId == gymId &&
            entity.MemberId == member.Id &&
            entity.TrainingSessionId == trainingSession.Id);

        if (existingBooking != null)
        {
            throw new ValidationAppException("This member already has a booking for the selected session.");
        }

        if (trainingSession.Status != TrainingSessionStatus.Published)
        {
            throw new ValidationAppException("Bookings can be created only for published sessions.");
        }

        var bookedCount = await dbContext.Bookings.CountAsync(entity =>
            entity.TrainingSessionId == trainingSession.Id &&
            entity.Status == BookingStatus.Booked);

        if (bookedCount >= trainingSession.Capacity)
        {
            throw new ValidationAppException("Training session capacity has been reached.");
        }

        var settings = await dbContext.GymSettings.FirstOrDefaultAsync(entity => entity.GymId == gymId)
                       ?? throw new NotFoundException("Gym settings were not found.");

        var chargedPrice = await membershipWorkflowService.CalculateBookingPriceAsync(gymId, member.Id, trainingSession);
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

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (paymentRequired)
        {
            dbContext.Payments.Add(new Payment
            {
                GymId = gymId,
                BookingId = booking.Id,
                Amount = chargedPrice,
                CurrencyCode = trainingSession.CurrencyCode,
                Status = PaymentStatus.Completed,
                Reference = request.PaymentReference
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToBookingResponse(booking);
    }

    public async Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Trainer);

        var booking = await dbContext.Bookings
            .Include(entity => entity.TrainingSession)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .FirstOrDefaultAsync(entity => entity.Id == bookingId)
            ?? throw new NotFoundException("Booking was not found.");

        await authorizationService.EnsureTrainingAttendanceAccessAsync(booking.TrainingSession!);

        booking.Status = request.Status;
        if (request.Status == BookingStatus.Cancelled)
        {
            booking.CancelledAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToBookingResponse(booking);
    }

    public async Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var booking = await dbContext.Bookings.FirstOrDefaultAsync(entity => entity.Id == id)
                      ?? throw new NotFoundException("Booking was not found.");
        await authorizationService.EnsureBookingAccessAsync(booking);
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TrainingSessionResponse> ProjectSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.TrainingSessions
            .FirstOrDefaultAsync(entity => entity.Id == sessionId)
            ?? throw new NotFoundException("Training session was not found.");

        var trainerContractIds = await dbContext.WorkShifts
            .Where(entity => entity.TrainingSessionId == sessionId && entity.ShiftType == ShiftType.Training)
            .Select(entity => entity.ContractId)
            .ToListAsync(cancellationToken);

        return new TrainingSessionResponse
        {
            Id = session.Id,
            CategoryId = session.CategoryId,
            Name = Translate(session.Name) ?? string.Empty,
            Description = Translate(session.Description),
            StartAtUtc = session.StartAtUtc,
            EndAtUtc = session.EndAtUtc,
            Capacity = session.Capacity,
            BasePrice = session.BasePrice,
            CurrencyCode = session.CurrencyCode,
            Status = session.Status,
            TrainerContractIds = trainerContractIds
        };
    }

    private static TrainingCategoryResponse ToCategoryResponse(TrainingCategory category)
    {
        return new TrainingCategoryResponse
        {
            Id = category.Id,
            Name = Translate(category.Name) ?? string.Empty,
            Description = Translate(category.Description)
        };
    }

    private static WorkShiftResponse ToShiftResponse(WorkShift shift)
    {
        return new WorkShiftResponse
        {
            Id = shift.Id,
            ContractId = shift.ContractId,
            StartAtUtc = shift.StartAtUtc,
            EndAtUtc = shift.EndAtUtc,
            ShiftType = shift.ShiftType,
            TrainingSessionId = shift.TrainingSessionId,
            Comment = shift.Comment
        };
    }

    private static BookingResponse ToBookingResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            TrainingSessionId = booking.TrainingSessionId,
            TrainingSessionName = Translate(booking.TrainingSession?.Name) ?? string.Empty,
            MemberId = booking.MemberId,
            MemberName = $"{booking.Member?.Person?.FirstName} {booking.Member?.Person?.LastName}".Trim(),
            MemberCode = booking.Member?.MemberCode ?? string.Empty,
            Status = booking.Status,
            ChargedPrice = booking.ChargedPrice,
            PaymentRequired = booking.PaymentRequired
        };
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
