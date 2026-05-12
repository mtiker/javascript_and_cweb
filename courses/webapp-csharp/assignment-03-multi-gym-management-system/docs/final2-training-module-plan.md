# Final-2 Training Module Plan

**Status:** Phase 20 training category workflow ownership completed. Session,
booking, and attendance HTTP adapters remain implemented through
`Modules.Training` mediator messages.

## Scope

Phase 20 moves the public tenant training category API workflow into
Training-module handlers while preserving the existing API contract. The
broader Phase 19 adapter migration keeps the public tenant training HTTP
workflow behind Training module mediator messages:

- `GET|POST|PUT|DELETE /api/v1/{gymCode}/training-categories`
- `GET /api/v1/{gymCode}/training-sessions`
- `GET /api/v1/{gymCode}/training-sessions/{id}`
- `POST|PUT|DELETE /api/v1/{gymCode}/training-sessions`
- `GET|POST /api/v1/{gymCode}/bookings`
- `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- `DELETE /api/v1/{gymCode}/bookings/{id}`

Finance, membership package, payment, invoice, maintenance, equipment, and
work-shift CRUD are intentionally not migrated in this phase.

## Implemented Shape

```text
HTTP request
  -> WebApp TrainingCategoriesController / TrainingSessionsController / BookingsController
  -> IMediator.SendAsync(...)
  -> Modules.Training.Application handler
     -> category handlers: authorization + UOW/repository + mapper
     -> session/booking handlers: ITrainingWorkflowService
```

Training category CRUD is now a module-owned workflow:
`ListTrainingCategoriesQueryHandler`, `CreateTrainingCategoryCommandHandler`,
`UpdateTrainingCategoryCommandHandler`, and
`DeleteTrainingCategoryCommandHandler` perform tenant authorization,
tenant-scoped repository lookup, validation, `LangStr` write-culture handling,
save/delete orchestration, and DTO projection directly in
`Modules.Training.Application`.

Session, booking, and attendance handlers remain route-preserving adapters to
`ITrainingWorkflowService` until their adjacent member/staff/finance lookup
contracts are stable.

## Behavioral Invariants

- training category CRUD keeps existing `LangStr` localization behavior
- session list/detail responses include assigned trainer contract IDs
- session create/update keeps category validation and trainer contract checks
- booking create prevents duplicate active bookings for the same member/session
- booking create keeps capacity, published-session, and payment-reference rules
- cancel marks the booking as `Cancelled` and stamps cancellation time
- attendance update is restricted to owner/admin or assigned trainer access
- unassigned trainers receive the existing forbidden response
- public route names, DTOs, status codes, and `ProblemDetails` behavior remain
  compatible with the React client

## Boundary Rules

- WebApp is the adapter/composition layer and may reference
  `Modules.Training.Contracts`.
- Training handlers are internal to `Modules.Training.Application`.
- Training does not reference `Modules.Users` or `Modules.GymManagement`
  internals.
- Cross-module user/member/staff context is reached through existing shared
  authorization services during this adapter phase. Future deeper migration
  should replace those BLL calls with explicit mediator lookups rather than
  direct module references.
- DTOs remain in `App.DTO.v1` until a deliberate API-version migration.

## Tests

Phase 20 is covered by:

- `TrainingModuleMediatorTests` for module-owned training category CRUD
  dispatch, tenant-scoped update/delete behavior, and remaining session/booking
  command/query dispatch through `AddTrainingModule`
- `TrainingWorkflowServiceTests` for category CRUD, `LangStr`, session
  list/detail, booking create/cancel, duplicate booking, attendance update, and
  unassigned-trainer forbidden behavior
- `TrainingCategoryLocalizationTests` for API-level category CRUD,
  `Accept-Language`, fallback localization, and validation `ProblemDetails`
- `TenantControllerTests.BookingsController_ForwardsParametersAndReturnsCurrentResultShapes`
- `AdditionalControllerTests.TrainingSessionsController_ForwardsParametersAndReturnsCurrentResultShapes`
- `ModuleArchitectureTests.TrainingModule_DoesNotReferenceUsersOrGymManagementInternals`
- `ModuleArchitectureTests.TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers`
- React Vitest coverage for training categories, sessions, booking, and trainer
  attendance

## Remaining Work

- Move remaining Training session, booking, attendance, work-shift, and coaching
  internals out of `App.BLL` after adjacent member/staff lookup contracts are
  stable.
- Add explicit Training-owned cross-module lookup messages for current member,
  trainer assignment, gym settings, and booking pricing when the finance and
  gym-management module phases expose those contracts.
- Keep membership finance and maintenance migrations in their own phases.
