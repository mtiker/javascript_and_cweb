using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Shared.Contracts.Dtos.v1.TrainingSessions;
using Shared.Contracts.ModuleApis;

namespace Modules.Training.Application.Mappers;

public interface ITrainingMapper
{
    TrainingCategoryResponse ToCategory(TrainingCategory category);

    IReadOnlyCollection<TrainingCategoryResponse> ToCategoryList(IEnumerable<TrainingCategory> categories);

    TrainingSessionResponse ToSession(TrainingSession session);

    BookingResponse ToBooking(Booking booking);

    BookingResponse ToBooking(Booking booking, MemberSummary member);

    IReadOnlyCollection<BookingResponse> ToBookingList(IEnumerable<Booking> bookings);

}
