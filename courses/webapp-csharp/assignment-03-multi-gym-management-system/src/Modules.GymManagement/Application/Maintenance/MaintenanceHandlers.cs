using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;
using BuildingBlocks.Mediator;
using Modules.GymManagement.Contracts;

namespace Modules.GymManagement.Application.Maintenance;

internal sealed class ListOpeningHoursQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListOpeningHoursQuery, IReadOnlyCollection<OpeningHoursResponse>>
{
    public Task<IReadOnlyCollection<OpeningHoursResponse>> HandleAsync(ListOpeningHoursQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetOpeningHoursAsync(request.GymCode, cancellationToken);
}

internal sealed class CreateOpeningHoursCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<CreateOpeningHoursCommand, OpeningHoursResponse>
{
    public Task<OpeningHoursResponse> HandleAsync(CreateOpeningHoursCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.CreateOpeningHoursAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class UpdateOpeningHoursCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateOpeningHoursCommand, OpeningHoursResponse>
{
    public Task<OpeningHoursResponse> HandleAsync(UpdateOpeningHoursCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateOpeningHoursAsync(request.GymCode, request.OpeningHoursId, request.Request, cancellationToken);
}

internal sealed class DeleteOpeningHoursCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteOpeningHoursCommand>
{
    public Task HandleAsync(DeleteOpeningHoursCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteOpeningHoursAsync(request.GymCode, request.OpeningHoursId, cancellationToken);
}

internal sealed class ListOpeningHourExceptionsQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListOpeningHourExceptionsQuery, IReadOnlyCollection<OpeningHoursExceptionResponse>>
{
    public Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> HandleAsync(ListOpeningHourExceptionsQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetOpeningHourExceptionsAsync(request.GymCode, cancellationToken);
}

internal sealed class CreateOpeningHourExceptionCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<CreateOpeningHourExceptionCommand, OpeningHoursExceptionResponse>
{
    public Task<OpeningHoursExceptionResponse> HandleAsync(CreateOpeningHourExceptionCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.CreateOpeningHourExceptionAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class UpdateOpeningHourExceptionCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateOpeningHourExceptionCommand, OpeningHoursExceptionResponse>
{
    public Task<OpeningHoursExceptionResponse> HandleAsync(UpdateOpeningHourExceptionCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateOpeningHourExceptionAsync(request.GymCode, request.ExceptionId, request.Request, cancellationToken);
}

internal sealed class DeleteOpeningHourExceptionCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteOpeningHourExceptionCommand>
{
    public Task HandleAsync(DeleteOpeningHourExceptionCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteOpeningHourExceptionAsync(request.GymCode, request.ExceptionId, cancellationToken);
}

internal sealed class ListEquipmentModelsQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListEquipmentModelsQuery, IReadOnlyCollection<EquipmentModelResponse>>
{
    public Task<IReadOnlyCollection<EquipmentModelResponse>> HandleAsync(ListEquipmentModelsQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetEquipmentModelsAsync(request.GymCode, cancellationToken);
}

internal sealed class CreateEquipmentModelCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<CreateEquipmentModelCommand, EquipmentModelResponse>
{
    public Task<EquipmentModelResponse> HandleAsync(CreateEquipmentModelCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.CreateEquipmentModelAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class UpdateEquipmentModelCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateEquipmentModelCommand, EquipmentModelResponse>
{
    public Task<EquipmentModelResponse> HandleAsync(UpdateEquipmentModelCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateEquipmentModelAsync(request.GymCode, request.EquipmentModelId, request.Request, cancellationToken);
}

internal sealed class DeleteEquipmentModelCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteEquipmentModelCommand>
{
    public Task HandleAsync(DeleteEquipmentModelCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteEquipmentModelAsync(request.GymCode, request.EquipmentModelId, cancellationToken);
}

internal sealed class ListEquipmentQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListEquipmentQuery, IReadOnlyCollection<EquipmentResponse>>
{
    public Task<IReadOnlyCollection<EquipmentResponse>> HandleAsync(ListEquipmentQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetEquipmentAsync(request.GymCode, cancellationToken);
}

internal sealed class CreateEquipmentCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<CreateEquipmentCommand, EquipmentResponse>
{
    public Task<EquipmentResponse> HandleAsync(CreateEquipmentCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.CreateEquipmentAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class UpdateEquipmentCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateEquipmentCommand, EquipmentResponse>
{
    public Task<EquipmentResponse> HandleAsync(UpdateEquipmentCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateEquipmentAsync(request.GymCode, request.EquipmentId, request.Request, cancellationToken);
}

internal sealed class DeleteEquipmentCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteEquipmentCommand>
{
    public Task HandleAsync(DeleteEquipmentCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteEquipmentAsync(request.GymCode, request.EquipmentId, cancellationToken);
}

internal sealed class ListMaintenanceTasksQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListMaintenanceTasksQuery, IReadOnlyCollection<MaintenanceTaskResponse>>
{
    public Task<IReadOnlyCollection<MaintenanceTaskResponse>> HandleAsync(ListMaintenanceTasksQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetMaintenanceTasksAsync(request.GymCode, cancellationToken);
}

internal sealed class CreateMaintenanceTaskCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<CreateMaintenanceTaskCommand, MaintenanceTaskResponse>
{
    public Task<MaintenanceTaskResponse> HandleAsync(CreateMaintenanceTaskCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.CreateTaskAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class UpdateMaintenanceTaskStatusCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateMaintenanceTaskStatusCommand, MaintenanceTaskResponse>
{
    public Task<MaintenanceTaskResponse> HandleAsync(UpdateMaintenanceTaskStatusCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateTaskStatusAsync(request.GymCode, request.TaskId, request.Request, cancellationToken);
}

internal sealed class UpdateMaintenanceTaskAssignmentCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateMaintenanceTaskAssignmentCommand, MaintenanceTaskResponse>
{
    public Task<MaintenanceTaskResponse> HandleAsync(UpdateMaintenanceTaskAssignmentCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateTaskAssignmentAsync(request.GymCode, request.TaskId, request.Request, cancellationToken);
}

internal sealed class ListMaintenanceTaskAssignmentHistoryQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListMaintenanceTaskAssignmentHistoryQuery, IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>
{
    public Task<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>> HandleAsync(ListMaintenanceTaskAssignmentHistoryQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetTaskAssignmentHistoryAsync(request.GymCode, request.TaskId, cancellationToken);
}

internal sealed class GenerateDueMaintenanceTasksCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<GenerateDueMaintenanceTasksCommand, Message>
{
    public async Task<Message> HandleAsync(GenerateDueMaintenanceTasksCommand request, CancellationToken cancellationToken)
    {
        var created = await maintenanceWorkflowService.GenerateDueScheduledTasksAsync(request.GymCode, cancellationToken);
        return new Message($"Created {created} scheduled maintenance tasks.");
    }
}

internal sealed class DeleteMaintenanceTaskCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteMaintenanceTaskCommand>
{
    public Task HandleAsync(DeleteMaintenanceTaskCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteMaintenanceTaskAsync(request.GymCode, request.TaskId, cancellationToken);
}

internal sealed class GetGymSettingsQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<GetGymSettingsQuery, GymSettingsResponse>
{
    public Task<GymSettingsResponse> HandleAsync(GetGymSettingsQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetGymSettingsAsync(request.GymCode, cancellationToken);
}

internal sealed class UpdateGymSettingsCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpdateGymSettingsCommand, GymSettingsResponse>
{
    public Task<GymSettingsResponse> HandleAsync(UpdateGymSettingsCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpdateGymSettingsAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class ListGymUsersQueryHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<ListGymUsersQuery, IReadOnlyCollection<GymUserResponse>>
{
    public Task<IReadOnlyCollection<GymUserResponse>> HandleAsync(ListGymUsersQuery request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.GetGymUsersAsync(request.GymCode, cancellationToken);
}

internal sealed class UpsertGymUserCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<UpsertGymUserCommand, GymUserResponse>
{
    public Task<GymUserResponse> HandleAsync(UpsertGymUserCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.UpsertGymUserAsync(request.GymCode, request.Request, cancellationToken);
}

internal sealed class DeleteGymUserCommandHandler(IMaintenanceWorkflowService maintenanceWorkflowService)
    : IRequestHandler<DeleteGymUserCommand>
{
    public Task HandleAsync(DeleteGymUserCommand request, CancellationToken cancellationToken) =>
        maintenanceWorkflowService.DeleteGymUserAsync(request.GymCode, request.AppUserId, request.RoleName, cancellationToken);
}
