using App.Domain.Entities;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.WorkShifts;

namespace App.BLL.Mapping;

public interface ITrainingMapper
{
    TrainingCategoryResponse ToCategory(TrainingCategory category);

    IReadOnlyCollection<TrainingCategoryResponse> ToCategoryList(IEnumerable<TrainingCategory> categories);

    TrainingSessionResponse ToSession(TrainingSession session, IEnumerable<Guid> trainerContractIds);

    BookingResponse ToBooking(Booking booking);

    IReadOnlyCollection<BookingResponse> ToBookingList(IEnumerable<Booking> bookings);

    WorkShiftResponse ToWorkShift(WorkShift workShift);

    IReadOnlyCollection<WorkShiftResponse> ToWorkShiftList(IEnumerable<WorkShift> workShifts);
}
