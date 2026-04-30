# Final1 Training Slice Plan

## Status

Phase 12 - Training categories, sessions, work shifts, bookings, and trainer
attendance migrated to the mandatory Final1 shape: dedicated repository
contracts + Unit of Work + application service + dedicated mapper.

The public API contract is unchanged:

- `GET    /api/v1/{gymCode}/training-categories`
- `POST   /api/v1/{gymCode}/training-categories`
- `PUT    /api/v1/{gymCode}/training-categories/{id}`
- `DELETE /api/v1/{gymCode}/training-categories/{id}`
- `GET    /api/v1/{gymCode}/training-sessions`
- `GET    /api/v1/{gymCode}/training-sessions/{id}`
- `POST   /api/v1/{gymCode}/training-sessions`
- `PUT    /api/v1/{gymCode}/training-sessions/{id}`
- `DELETE /api/v1/{gymCode}/training-sessions/{id}`
- `GET    /api/v1/{gymCode}/work-shifts`
- `POST   /api/v1/{gymCode}/work-shifts`
- `PUT    /api/v1/{gymCode}/work-shifts/{id}`
- `DELETE /api/v1/{gymCode}/work-shifts/{id}`
- `GET    /api/v1/{gymCode}/bookings`
- `POST   /api/v1/{gymCode}/bookings`
- `PUT    /api/v1/{gymCode}/bookings/{id}/attendance`
- `DELETE /api/v1/{gymCode}/bookings/{id}`

The MVC pages and React training/booking pages keep their existing routes,
view models, and request/response shapes.

No membership/finance code is migrated in this phase.

## Course Context

This aligns the Training slice of Assignment 03 with the Final1 Clean/Onion
requirements:

- API and MVC controllers are boundary adapters.
- Training and booking use cases live in the application/BLL layer.
- Training-domain persistence details live behind BLL contracts.
- The EF implementations live in infrastructure (`App.DAL.EF`).
- Entity-to-DTO projection lives in a mapper, not in the service.

## Implemented Boundary

| Concern | Final location |
|---|---|
| Training use cases | `src/App.BLL/Services/TrainingWorkflowService.cs` |
| Training categories persistence contract | `src/App.BLL/Contracts/Persistence/ITrainingCategoryRepository.cs` |
| Training sessions persistence contract | `src/App.BLL/Contracts/Persistence/ITrainingSessionRepository.cs` |
| Bookings persistence contract | `src/App.BLL/Contracts/Persistence/IBookingRepository.cs` |
| Work shifts persistence contract | `src/App.BLL/Contracts/Persistence/IWorkShiftRepository.cs` |
| EF training category implementation | `src/App.DAL.EF/Repositories/EfTrainingCategoryRepository.cs` |
| EF training session implementation | `src/App.DAL.EF/Repositories/EfTrainingSessionRepository.cs` |
| EF booking implementation | `src/App.DAL.EF/Repositories/EfBookingRepository.cs` |
| EF work shift implementation | `src/App.DAL.EF/Repositories/EfWorkShiftRepository.cs` |
| Unit of Work training access | `IAppUnitOfWork.TrainingCategories` / `.TrainingSessions` / `.Bookings` / `.WorkShifts` |
| Training response mapping | `src/App.BLL/Mapping/TrainingMapper.cs` |
| API controller delegation | `src/WebApp/ApiControllers/Tenant/TrainingCategoriesController.cs`, `TrainingSessionsController.cs`, `BookingsController.cs`, `WorkShiftsController.cs` |
| DI wiring | `src/WebApp/Setup/ServiceExtensions.cs`, `src/App.DAL.EF/PersistenceServiceExtensions.cs` |

## Request Flow

List training categories:

1. `TrainingCategoriesController.GetCategories` receives `(gymCode)`.
2. Controller delegates to `ITrainingWorkflowService.GetCategoriesAsync`.
3. Service ensures `Owner`/`Admin`/`Member`/`Trainer` access via
   `IAuthorizationService.EnsureTenantAccessAsync`.
4. Service calls `IAppUnitOfWork.TrainingCategories.ListByGymAsync(gymId)`.
5. `ITrainingMapper.ToCategoryList(...)` projects each `TrainingCategory` to
   `TrainingCategoryResponse` (LangStr translated against
   `CultureInfo.CurrentUICulture`).

Create / update training category:

1. Service ensures `Owner`/`Admin` access.
2. Request normalized + validated (name required).
3. For update: load via `ITrainingCategoryRepository.FindAsync(gymId, id)`
   (404 on miss).
4. New `TrainingCategory` is staged via `AddAsync`, or properties mutated
   on the tracked graph.
5. `IAppUnitOfWork.SaveChangesAsync` commits.
6. Mapper projects the result.

Delete training category:

1. Service ensures `Owner`/`Admin` access.
2. Service loads via `ITrainingCategoryRepository.FindAsync(gymId, id)` (404
   on miss).
3. `Remove` stages the delete; `SaveChangesAsync` commits - the
   `AppDbContext.ApplySoftDelete()` rewrite turns the delete into
   `IsDeleted = true` for `TenantBaseEntity` rows.

List / get training session:

1. Service ensures tenant access.
2. `ITrainingSessionRepository.ListByGymAsync(gymId)` loads sessions.
3. For each session, the service calls
   `IWorkShiftRepository.ListTrainerContractIdsForSessionAsync(gymId, sessionId)` to gather
   trainer contract IDs.
4. Mapper projects to `TrainingSessionResponse`.

Create / update training session:

1. Service ensures `Owner`/`Admin` access.
2. End time validated against start time.
3. Category presence checked via
   `ITrainingCategoryRepository.FindAsync(gymId, request.CategoryId)`.
4. Trainer contracts validated via the generic
   `IRepository<EmploymentContract, Guid>.ListAsync` over
   `(GymId == gymId && trainerContractIds.Contains(Id))`.
5. For new sessions: `ISubscriptionTierLimitService.EnsureCanCreateTrainingSessionAsync`,
   then `AddAsync`.
6. For updates: properties mutated on tracked graph.
7. `SaveChangesAsync` commits.
8. Existing trainer shifts removed and re-created from the request.

List bookings:

1. Service ensures tenant access (`Owner`/`Admin`/`Member`/`Trainer`).
2. The current actor's role is read from `IUserContextService`.
3. The repository selector method picks the appropriate query:
   - `ListByGymAsync(gymId)` for owner/admin.
   - `ListForMemberAsync(gymId, memberId)` for the `Member` role.
   - `ListForTrainerAsync(gymId, staffId)` for the `Trainer` role -
     filters by sessions assigned to that trainer through `WorkShifts`.
4. Mapper projects the rows.

Create booking:

1. Service ensures `Owner`/`Admin`/`Member` access.
2. `ITrainingSessionRepository.FindAsync(gymId, id)` (404).
3. `IMemberRepository.FindWithPersonAsync(gymId, memberId)` (404).
4. `IAuthorizationService.EnsureMemberSelfAccessAsync` for the resolved
   member (members can book only their own session).
5. Duplicate booking check via
   `IBookingRepository.ExistsForMemberSessionAsync`.
6. Status check (must be `Published`).
7. Capacity check via `IBookingRepository.CountActiveForSessionAsync`.
8. GymSettings loaded via the generic `IRepository<GymSettings, Guid>` to
   evaluate `AllowNonMemberBookings`.
9. `IMembershipWorkflowService.CalculateBookingPriceAsync` decides charged
   price.
10. Payment reference enforced when payment is required.
11. `Booking` added through `IBookingRepository.AddAsync`.
12. If payment required, `Payment` added through
    `IRepository<Payment, Guid>.AddAsync`.
13. `SaveChangesAsync` commits.

Update attendance (trainer / admin):

1. Service ensures `Owner`/`Admin`/`Trainer` access.
2. `IBookingRepository.FindWithTrainingSessionAndMemberAsync(gymId, id)` (404).
3. `IAuthorizationService.EnsureTrainingAttendanceAccessAsync(session)` -
   trainer must be assigned to the session's training shift, otherwise
   `ForbiddenException`.
4. Status mutated; `CancelledAtUtc` stamped on cancellation.
5. `SaveChangesAsync` commits.

Cancel booking:

1. Service ensures `Owner`/`Admin`/`Member` access.
2. `IBookingRepository.FindAsync(gymId, id)` (404).
3. `IAuthorizationService.EnsureBookingAccessAsync(booking)` - admin always
   allowed; member must own the booking; trainer must be assigned.
4. Status set to `Cancelled`; `CancelledAtUtc` stamped.
5. `SaveChangesAsync` commits.

## Tests

Coverage added in this slice (and existing tests preserved):

- `TrainingWorkflowServiceTests.GetCategoriesAsync_ReturnsCategoriesOrderedByValidFrom`
- `TrainingWorkflowServiceTests.CreateCategoryAsync_RejectsBlankName`
- `TrainingWorkflowServiceTests.CreateCategoryAsync_PersistsThroughUnitOfWork`
- `TrainingWorkflowServiceTests.UpdateCategoryAsync_TenantScopedNotFound`
- `TrainingWorkflowServiceTests.DeleteCategoryAsync_RemovesCategory`
- `TrainingWorkflowServiceTests.GetCategoriesAsync_TranslatesLangStrToActiveCulture`
- `TrainingWorkflowServiceTests.CreateBookingAsync_PersistsBookingForPublishedSession`
- `TrainingWorkflowServiceTests.CreateBookingAsync_RejectsDuplicateBookingForMember`
- `TrainingWorkflowServiceTests.CancelBookingAsync_StampsCancelledTimestamp`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_AllowsAssignedTrainer`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_RejectsTrainerForUnassignedSession`
- `TrainingCategoryLocalizationTests.*` - preserved API regression suite
  (training category CRUD + LangStr).
- `ArchitectureTests.TrainingSlice_UsesDedicatedRepositoryAndMapperBoundaries` -
  service composition + namespace assertion.
- `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence`
  extended with `ITrainingCategoryRepository`, `ITrainingSessionRepository`,
  `IBookingRepository`, `IWorkShiftRepository`.

## Out Of Scope

- Membership packages, memberships, payments, invoices (Phase 13+).
- Coaching plans (separate slice).
- Maintenance tasks.
- Identity model changes.
- Training API or DTO contract changes.
- Removing `IAppDbContext` from BLL.

## Notes

- `TrainingWorkflowService` keeps its current name for continuity. The class
  no longer accepts `IAppDbContext`; the constructor is now
  `(IAppUnitOfWork, IAuthorizationService, IUserContextService,
  IMembershipWorkflowService, ISubscriptionTierLimitService, ITrainingMapper)`.
- The booking and trainer-attendance authorization rules continue to live in
  `ResourceAuthorizationChecker` because they are cross-cutting authorization
  concerns and independent of the training slice migration. The slice plan
  audits them in `docs/booking-rules-audit.md` and
  `docs/trainer-authorization-audit.md`.
- `LangStr` translation is performed in the mapper using
  `CultureInfo.CurrentUICulture`, matching the localization contract in
  `docs/langstr-contract.md`.
