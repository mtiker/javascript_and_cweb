using System.Globalization;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;

namespace App.BLL.Mapping;

public sealed class TrainingMapper : ITrainingMapper
{
    public TrainingCategoryResponse ToCategory(TrainingCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new TrainingCategoryResponse
        {
            Id = category.Id,
            Name = Translate(category.Name) ?? category.Name.ToString(),
            Description = Translate(category.Description) ?? category.Description?.ToString()
        };
    }

    public IReadOnlyCollection<TrainingCategoryResponse> ToCategoryList(IEnumerable<TrainingCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        return categories.Select(ToCategory).ToArray();
    }

    public TrainingSessionResponse ToSession(TrainingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new TrainingSessionResponse
        {
            Id = session.Id,
            CategoryId = session.CategoryId,
            Name = Translate(session.Name) ?? session.Name.ToString(),
            Description = Translate(session.Description) ?? session.Description?.ToString(),
            StartAtUtc = session.StartAtUtc,
            EndAtUtc = session.EndAtUtc,
            Capacity = session.Capacity,
            BasePrice = session.BasePrice,
            CurrencyCode = session.CurrencyCode,
            Status = session.Status,
            TrainerStaffId = session.TrainerStaffId,
            TrainerName = FormatStaffName(session.TrainerStaff)
        };
    }

    public BookingResponse ToBooking(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return new BookingResponse
        {
            Id = booking.Id,
            TrainingSessionId = booking.TrainingSessionId,
            TrainingSessionName = Translate(booking.TrainingSession?.Name) ?? booking.TrainingSession?.Name.ToString() ?? string.Empty,
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

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }

    private static string? FormatStaffName(Staff? staff)
    {
        return staff == null
            ? null
            : $"{staff.Person?.FirstName} {staff.Person?.LastName}".Trim();
    }
}
