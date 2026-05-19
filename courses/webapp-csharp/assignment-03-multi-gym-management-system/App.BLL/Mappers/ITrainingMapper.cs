using App.Domain.Entities;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;

namespace App.BLL.Mappers;

public interface ITrainingMapper
{
    TrainingCategoryResponse ToCategory(TrainingCategory category);

    IReadOnlyCollection<TrainingCategoryResponse> ToCategoryList(IEnumerable<TrainingCategory> categories);

    TrainingSessionResponse ToSession(TrainingSession session);

    BookingResponse ToBooking(Booking booking);

    IReadOnlyCollection<BookingResponse> ToBookingList(IEnumerable<Booking> bookings);

}
