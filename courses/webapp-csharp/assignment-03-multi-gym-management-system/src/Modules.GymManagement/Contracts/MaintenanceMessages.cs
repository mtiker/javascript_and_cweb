using App.DTO.v1;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;
using BuildingBlocks.Mediator;

namespace Modules.GymManagement.Contracts;

public sealed record ListOpeningHoursQuery(string GymCode) : IRequest<IReadOnlyCollection<OpeningHoursResponse>>;

public sealed record CreateOpeningHoursCommand(string GymCode, OpeningHoursUpsertRequest Request) : IRequest<OpeningHoursResponse>;

public sealed record UpdateOpeningHoursCommand(string GymCode, Guid OpeningHoursId, OpeningHoursUpsertRequest Request) : IRequest<OpeningHoursResponse>;

public sealed record DeleteOpeningHoursCommand(string GymCode, Guid OpeningHoursId) : IRequest;

public sealed record ListOpeningHourExceptionsQuery(string GymCode) : IRequest<IReadOnlyCollection<OpeningHoursExceptionResponse>>;

public sealed record CreateOpeningHourExceptionCommand(string GymCode, OpeningHoursExceptionUpsertRequest Request) : IRequest<OpeningHoursExceptionResponse>;

public sealed record UpdateOpeningHourExceptionCommand(string GymCode, Guid ExceptionId, OpeningHoursExceptionUpsertRequest Request) : IRequest<OpeningHoursExceptionResponse>;

public sealed record DeleteOpeningHourExceptionCommand(string GymCode, Guid ExceptionId) : IRequest;

public sealed record ListEquipmentModelsQuery(string GymCode) : IRequest<IReadOnlyCollection<EquipmentModelResponse>>;

public sealed record CreateEquipmentModelCommand(string GymCode, EquipmentModelUpsertRequest Request) : IRequest<EquipmentModelResponse>;

public sealed record UpdateEquipmentModelCommand(string GymCode, Guid EquipmentModelId, EquipmentModelUpsertRequest Request) : IRequest<EquipmentModelResponse>;

public sealed record DeleteEquipmentModelCommand(string GymCode, Guid EquipmentModelId) : IRequest;

public sealed record ListEquipmentQuery(string GymCode) : IRequest<IReadOnlyCollection<EquipmentResponse>>;

public sealed record CreateEquipmentCommand(string GymCode, EquipmentUpsertRequest Request) : IRequest<EquipmentResponse>;

public sealed record UpdateEquipmentCommand(string GymCode, Guid EquipmentId, EquipmentUpsertRequest Request) : IRequest<EquipmentResponse>;

public sealed record DeleteEquipmentCommand(string GymCode, Guid EquipmentId) : IRequest;

public sealed record ListMaintenanceTasksQuery(string GymCode) : IRequest<IReadOnlyCollection<MaintenanceTaskResponse>>;

public sealed record CreateMaintenanceTaskCommand(string GymCode, MaintenanceTaskUpsertRequest Request) : IRequest<MaintenanceTaskResponse>;

public sealed record UpdateMaintenanceTaskStatusCommand(string GymCode, Guid TaskId, MaintenanceStatusUpdateRequest Request) : IRequest<MaintenanceTaskResponse>;

public sealed record UpdateMaintenanceTaskAssignmentCommand(string GymCode, Guid TaskId, MaintenanceAssignmentUpdateRequest Request) : IRequest<MaintenanceTaskResponse>;

public sealed record ListMaintenanceTaskAssignmentHistoryQuery(string GymCode, Guid TaskId) : IRequest<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>;

public sealed record GenerateDueMaintenanceTasksCommand(string GymCode) : IRequest<Message>;

public sealed record DeleteMaintenanceTaskCommand(string GymCode, Guid TaskId) : IRequest;

public sealed record GetGymSettingsQuery(string GymCode) : IRequest<GymSettingsResponse>;

public sealed record UpdateGymSettingsCommand(string GymCode, GymSettingsUpdateRequest Request) : IRequest<GymSettingsResponse>;

public sealed record ListGymUsersQuery(string GymCode) : IRequest<IReadOnlyCollection<GymUserResponse>>;

public sealed record UpsertGymUserCommand(string GymCode, GymUserUpsertRequest Request) : IRequest<GymUserResponse>;

public sealed record DeleteGymUserCommand(string GymCode, Guid AppUserId, string RoleName) : IRequest;
