using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using BuildingBlocks.Mediator;

namespace Modules.Training.Contracts;

public sealed record ListTrainingCategoriesQuery(string GymCode) : IRequest<IReadOnlyCollection<TrainingCategoryResponse>>;

public sealed record CreateTrainingCategoryCommand(string GymCode, TrainingCategoryUpsertRequest Request) : IRequest<TrainingCategoryResponse>;

public sealed record UpdateTrainingCategoryCommand(string GymCode, Guid CategoryId, TrainingCategoryUpsertRequest Request) : IRequest<TrainingCategoryResponse>;

public sealed record DeleteTrainingCategoryCommand(string GymCode, Guid CategoryId) : IRequest;

public sealed record ListTrainingSessionsQuery(string GymCode) : IRequest<IReadOnlyCollection<TrainingSessionResponse>>;

public sealed record GetTrainingSessionQuery(string GymCode, Guid SessionId) : IRequest<TrainingSessionResponse>;

public sealed record CreateTrainingSessionCommand(string GymCode, TrainingSessionUpsertRequest Request) : IRequest<TrainingSessionResponse>;

public sealed record UpdateTrainingSessionCommand(string GymCode, Guid SessionId, TrainingSessionUpsertRequest Request) : IRequest<TrainingSessionResponse>;

public sealed record DeleteTrainingSessionCommand(string GymCode, Guid SessionId) : IRequest;

public sealed record ListBookingsQuery(string GymCode) : IRequest<IReadOnlyCollection<BookingResponse>>;

public sealed record CreateBookingCommand(string GymCode, BookingCreateRequest Request) : IRequest<BookingResponse>;

public sealed record CancelBookingCommand(string GymCode, Guid BookingId) : IRequest;

public sealed record UpdateBookingAttendanceCommand(string GymCode, Guid BookingId, AttendanceUpdateRequest Request) : IRequest<BookingResponse>;
