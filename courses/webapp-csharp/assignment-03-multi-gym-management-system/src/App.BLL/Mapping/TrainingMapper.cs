using System.Globalization;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.WorkShifts;

namespace App.BLL.Mapping;

public sealed class TrainingMapper : ITrainingMapper
{
    public TrainingCategoryResponse ToCategory(TrainingCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new TrainingCategoryResponse
        {
            Id = category.Id,
            Name = Translate(category.Name) ?? string.Empty,
            Description = Translate(category.Description)
        };
    }

    public IReadOnlyCollection<TrainingCategoryResponse> ToCategoryList(IEnumerable<TrainingCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        return categories.Select(ToCategory).ToArray();
    }

    public TrainingSessionResponse ToSession(TrainingSession session, IEnumerable<Guid> trainerContractIds)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(trainerContractIds);
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
            TrainerContractIds = trainerContractIds.ToList()
        };
    }

    public BookingResponse ToBooking(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
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

    public IReadOnlyCollection<BookingResponse> ToBookingList(IEnumerable<Booking> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        return bookings.Select(ToBooking).ToArray();
    }

    public WorkShiftResponse ToWorkShift(WorkShift workShift)
    {
        ArgumentNullException.ThrowIfNull(workShift);
        return new WorkShiftResponse
        {
            Id = workShift.Id,
            ContractId = workShift.ContractId,
            StartAtUtc = workShift.StartAtUtc,
            EndAtUtc = workShift.EndAtUtc,
            ShiftType = workShift.ShiftType,
            TrainingSessionId = workShift.TrainingSessionId,
            Comment = workShift.Comment
        };
    }

    public IReadOnlyCollection<WorkShiftResponse> ToWorkShiftList(IEnumerable<WorkShift> workShifts)
    {
        ArgumentNullException.ThrowIfNull(workShifts);
        return workShifts.Select(ToWorkShift).ToArray();
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
