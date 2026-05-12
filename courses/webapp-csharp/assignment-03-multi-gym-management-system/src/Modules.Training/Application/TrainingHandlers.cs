using App.BLL.Services;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingSessions;
using BuildingBlocks.Mediator;
using Modules.Training.Contracts;

namespace Modules.Training.Application;

internal sealed class ListTrainingSessionsQueryHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<ListTrainingSessionsQuery, IReadOnlyCollection<TrainingSessionResponse>>
{
    public Task<IReadOnlyCollection<TrainingSessionResponse>> HandleAsync(ListTrainingSessionsQuery request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.GetSessionsAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class GetTrainingSessionQueryHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<GetTrainingSessionQuery, TrainingSessionResponse>
{
    public Task<TrainingSessionResponse> HandleAsync(GetTrainingSessionQuery request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.GetSessionAsync(request.GymCode, request.SessionId, cancellationToken);
    }
}

internal sealed class CreateTrainingSessionCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<CreateTrainingSessionCommand, TrainingSessionResponse>
{
    public Task<TrainingSessionResponse> HandleAsync(CreateTrainingSessionCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.UpsertTrainingSessionAsync(request.GymCode, null, request.Request, cancellationToken);
    }
}

internal sealed class UpdateTrainingSessionCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<UpdateTrainingSessionCommand, TrainingSessionResponse>
{
    public Task<TrainingSessionResponse> HandleAsync(UpdateTrainingSessionCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.UpsertTrainingSessionAsync(request.GymCode, request.SessionId, request.Request, cancellationToken);
    }
}

internal sealed class DeleteTrainingSessionCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<DeleteTrainingSessionCommand>
{
    public Task HandleAsync(DeleteTrainingSessionCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.DeleteSessionAsync(request.GymCode, request.SessionId, cancellationToken);
    }
}

internal sealed class ListBookingsQueryHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<ListBookingsQuery, IReadOnlyCollection<BookingResponse>>
{
    public Task<IReadOnlyCollection<BookingResponse>> HandleAsync(ListBookingsQuery request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.GetBookingsAsync(request.GymCode, cancellationToken);
    }
}

internal sealed class CreateBookingCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<CreateBookingCommand, BookingResponse>
{
    public Task<BookingResponse> HandleAsync(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.CreateBookingAsync(request.GymCode, request.Request, cancellationToken);
    }
}

internal sealed class UpdateBookingAttendanceCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<UpdateBookingAttendanceCommand, BookingResponse>
{
    public Task<BookingResponse> HandleAsync(UpdateBookingAttendanceCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.UpdateAttendanceAsync(request.GymCode, request.BookingId, request.Request, cancellationToken);
    }
}

internal sealed class CancelBookingCommandHandler(ITrainingWorkflowService trainingWorkflowService)
    : IRequestHandler<CancelBookingCommand>
{
    public Task HandleAsync(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        return trainingWorkflowService.CancelBookingAsync(request.GymCode, request.BookingId, cancellationToken);
    }
}
