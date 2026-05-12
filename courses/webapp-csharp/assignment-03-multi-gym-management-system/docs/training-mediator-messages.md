# Training Mediator Messages

**Status:** Implemented for Phase 19 Training adapter migration.

## Message Flow

```text
HTTP request
  -> WebApp tenant controller
  -> IMediator.SendAsync(...)
  -> Modules.Training.Application.TrainingHandlers
  -> module-owned handler workflow
  -> Unit of Work + repositories
```

Controllers keep API versioning, Swagger metadata, route templates,
`Created`/`CreatedAtAction`, and `NoContent` behavior.

Training category CRUD is no longer only a route adapter around
`ITrainingWorkflowService`: its handlers own authorization, tenant-scoped
repository access, validation, `LangStr` write-culture handling, persistence,
and DTO projection. Session, booking, and attendance messages still delegate to
the existing shared workflow service during the transitional module phase.

## Categories

### `ListTrainingCategoriesQuery`

- Endpoint: `GET /api/v1/{gymCode}/training-categories`
- Handler: `ListTrainingCategoriesQueryHandler`
- Workflow: module-owned handler using `IAppUnitOfWork.TrainingCategories`
- Invariants:
  - caller must have tenant access as owner, admin, member, or trainer
  - names/descriptions are translated through `LangStr` using current UI culture

### `CreateTrainingCategoryCommand`

- Endpoint: `POST /api/v1/{gymCode}/training-categories`
- Handler: `CreateTrainingCategoryCommandHandler`
- Workflow: module-owned handler using `IAppUnitOfWork.TrainingCategories`
- Invariants:
  - caller must be owner/admin
  - blank names return validation `ProblemDetails`
  - stored `LangStr` uses active request culture

### `UpdateTrainingCategoryCommand`

- Endpoint: `PUT /api/v1/{gymCode}/training-categories/{id}`
- Handler: `UpdateTrainingCategoryCommandHandler`
- Workflow: module-owned handler using `IAppUnitOfWork.TrainingCategories`
- Invariants:
  - lookup is tenant-scoped
  - blank names are rejected
  - localized fields are replaced using active request culture

### `DeleteTrainingCategoryCommand`

- Endpoint: `DELETE /api/v1/{gymCode}/training-categories/{id}`
- Handler: `DeleteTrainingCategoryCommandHandler`
- Workflow: module-owned handler using `IAppUnitOfWork.TrainingCategories`
- Invariants:
  - lookup is tenant-scoped
  - successful response remains `204 No Content`

## Sessions

### `ListTrainingSessionsQuery`

- Endpoint: `GET /api/v1/{gymCode}/training-sessions`
- Handler: `ListTrainingSessionsQueryHandler`
- Service: `ITrainingWorkflowService.GetSessionsAsync`
- Invariants:
  - tenant roles can list sessions
  - trainer contract IDs are read from training work shifts

### `GetTrainingSessionQuery`

- Endpoint: `GET /api/v1/{gymCode}/training-sessions/{id}`
- Handler: `GetTrainingSessionQueryHandler`
- Service: `ITrainingWorkflowService.GetSessionAsync`
- Invariants:
  - lookup is tenant-scoped
  - missing or foreign session IDs return the existing not-found response

### `CreateTrainingSessionCommand` / `UpdateTrainingSessionCommand`

- Endpoint: `POST|PUT /api/v1/{gymCode}/training-sessions`
- Handler: `CreateTrainingSessionCommandHandler` /
  `UpdateTrainingSessionCommandHandler`
- Service: `ITrainingWorkflowService.UpsertTrainingSessionAsync`
- Invariants:
  - caller must be owner/admin
  - end time must be after start time
  - category and trainer contracts must belong to the tenant
  - create checks the subscription session limit
  - trainer assignment shifts are recreated for the session

### `DeleteTrainingSessionCommand`

- Endpoint: `DELETE /api/v1/{gymCode}/training-sessions/{id}`
- Handler: `DeleteTrainingSessionCommandHandler`
- Service: `ITrainingWorkflowService.DeleteSessionAsync`
- Invariants:
  - caller must be owner/admin
  - lookup is tenant-scoped

## Bookings And Attendance

### `ListBookingsQuery`

- Endpoint: `GET /api/v1/{gymCode}/bookings`
- Handler: `ListBookingsQueryHandler`
- Service: `ITrainingWorkflowService.GetBookingsAsync`
- Invariants:
  - members see their own bookings
  - trainers see bookings for assigned sessions
  - owners/admins see tenant bookings

### `CreateBookingCommand`

- Endpoint: `POST /api/v1/{gymCode}/bookings`
- Handler: `CreateBookingCommandHandler`
- Service: `ITrainingWorkflowService.CreateBookingAsync`
- Invariants:
  - session must be published
  - member must exist in tenant and pass self-access rules
  - duplicate member/session bookings are rejected
  - capacity is enforced
  - payment reference is required when payment is due

### `UpdateBookingAttendanceCommand`

- Endpoint: `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- Handler: `UpdateBookingAttendanceCommandHandler`
- Service: `ITrainingWorkflowService.UpdateAttendanceAsync`
- Invariants:
  - owner/admin can update tenant booking attendance
  - trainers can update only assigned session attendance
  - cancelled attendance status stamps cancellation time

### `CancelBookingCommand`

- Endpoint: `DELETE /api/v1/{gymCode}/bookings/{id}`
- Handler: `CancelBookingCommandHandler`
- Service: `ITrainingWorkflowService.CancelBookingAsync`
- Invariants:
  - owner/admin/member access is enforced
  - booking access is self-scoped for members
  - successful response remains `204 No Content`

## Registration

`AddTrainingModule` registers all Training handlers with
`AddModuleMediatorHandlersFromAssembly`. `AddAppModules` registers
`AddBuildingBlocks()` before modules so `IMediator` can resolve handlers.
