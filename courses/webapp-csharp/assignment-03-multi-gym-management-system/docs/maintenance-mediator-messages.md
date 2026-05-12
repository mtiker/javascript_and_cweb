# Maintenance Mediator Messages

Maintenance mediator contracts live in
`src/Modules.GymManagement/Contracts/MaintenanceMessages.cs`. Handlers live in
`src/Modules.GymManagement/Application/Maintenance/MaintenanceHandlers.cs`.

## Flow

```text
WebApp controller
  -> IMediator.SendAsync(message)
  -> Modules.GymManagement.Application.Maintenance handler
  -> existing maintenance workflow service
```

## Maintenance Task Messages

| Message | Response | Purpose |
|---|---|---|
| `ListMaintenanceTasksQuery(string GymCode)` | `IReadOnlyCollection<MaintenanceTaskResponse>` | List maintenance tasks visible to the current role. |
| `CreateMaintenanceTaskCommand(string GymCode, MaintenanceTaskUpsertRequest Request)` | `MaintenanceTaskResponse` | Create a maintenance task. |
| `UpdateMaintenanceTaskStatusCommand(string GymCode, Guid TaskId, MaintenanceStatusUpdateRequest Request)` | `MaintenanceTaskResponse` | Update status; caretaker access is validated by the workflow. |
| `UpdateMaintenanceTaskAssignmentCommand(string GymCode, Guid TaskId, MaintenanceAssignmentUpdateRequest Request)` | `MaintenanceTaskResponse` | Assign or reassign a task. |
| `ListMaintenanceTaskAssignmentHistoryQuery(string GymCode, Guid TaskId)` | `IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>` | Read assignment history. |
| `GenerateDueMaintenanceTasksCommand(string GymCode)` | `Message` | Generate due scheduled maintenance tasks. |
| `DeleteMaintenanceTaskCommand(string GymCode, Guid TaskId)` | none | Delete a maintenance task. |

## Facility Messages

| Message | Response | Purpose |
|---|---|---|
| `ListEquipmentModelsQuery(string GymCode)` | `IReadOnlyCollection<EquipmentModelResponse>` | List equipment models. |
| `CreateEquipmentModelCommand(...)` | `EquipmentModelResponse` | Create an equipment model. |
| `UpdateEquipmentModelCommand(...)` | `EquipmentModelResponse` | Update an equipment model. |
| `DeleteEquipmentModelCommand(...)` | none | Delete an equipment model. |
| `ListEquipmentQuery(string GymCode)` | `IReadOnlyCollection<EquipmentResponse>` | List equipment. |
| `CreateEquipmentCommand(...)` | `EquipmentResponse` | Create equipment. |
| `UpdateEquipmentCommand(...)` | `EquipmentResponse` | Update equipment. |
| `DeleteEquipmentCommand(...)` | none | Delete equipment. |

## Gym Operations Messages

| Message | Response | Purpose |
|---|---|---|
| `ListOpeningHoursQuery(string GymCode)` | `IReadOnlyCollection<OpeningHoursResponse>` | List weekly opening hours. |
| `CreateOpeningHoursCommand(...)` | `OpeningHoursResponse` | Create opening hours. |
| `UpdateOpeningHoursCommand(...)` | `OpeningHoursResponse` | Update opening hours. |
| `DeleteOpeningHoursCommand(...)` | none | Delete opening hours. |
| `ListOpeningHourExceptionsQuery(string GymCode)` | `IReadOnlyCollection<OpeningHoursExceptionResponse>` | List one-off schedule exceptions. |
| `CreateOpeningHourExceptionCommand(...)` | `OpeningHoursExceptionResponse` | Create an exception. |
| `UpdateOpeningHourExceptionCommand(...)` | `OpeningHoursExceptionResponse` | Update an exception. |
| `DeleteOpeningHourExceptionCommand(...)` | none | Delete an exception. |
| `GetGymSettingsQuery(string GymCode)` | `GymSettingsResponse` | Read gym settings. |
| `UpdateGymSettingsCommand(string GymCode, GymSettingsUpdateRequest Request)` | `GymSettingsResponse` | Update gym settings. |
| `ListGymUsersQuery(string GymCode)` | `IReadOnlyCollection<GymUserResponse>` | List tenant role assignments. |
| `UpsertGymUserCommand(string GymCode, GymUserUpsertRequest Request)` | `GymUserResponse` | Add or update a tenant role assignment. |
| `DeleteGymUserCommand(string GymCode, Guid AppUserId, string RoleName)` | none | Remove a tenant role assignment. |

## Registration

`AddGymManagementModule` scans the GymManagement assembly for handlers.
Maintenance remains in GymManagement for Final2 because equipment, staff,
settings, and maintenance tasks are one operational bounded context.
